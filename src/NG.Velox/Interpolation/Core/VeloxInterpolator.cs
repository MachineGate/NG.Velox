using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NG.Velox.Interpolation.Core
{
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Helpers;
    using NG.Velox.Interpolation.Data;
    using NG.Velox.Interpolation.Interfaces;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Memory.Core;
    using NG.Velox.Pipeline.Data;
    using NG.Velox.Planning.Data;

    /// <summary>
    /// Generates high-density trajectory points from PlannedBlocks using adaptive step sizing
    /// and S-curve velocity profiles via high-performance raw unmanaged pointer arithmetic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Adaptive step:</b>
    /// <list type="bullet">
    /// <item><description>Linear (G00/G01): fixed step up to MAX_LINEAR_STEP (0.1 mm)</description></item>
    /// <item><description>Arc (G02/G03): step based on chordal tolerance: <c>sqrt(8 * R * tolerance)</c></description></item>
    /// <item><description>Larger radius = larger step (flatter arc). Smaller tolerance = smaller step.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>S-curve polynomial:</b> <c>v = vStart + (vEnd - vStart) * (3τ² - 2τ³)</c>
    /// where τ ∈. This "smoothstep" ensures zero jerk at phase boundaries.
    /// </para>
    /// <para>
    /// <b>End-point clamping:</b> Last point of each block is forced to exact target
    /// coordinates to eliminate float drift (critical for trajectory continuity).
    /// </para>
    /// </remarks>
    internal sealed unsafe class VeloxInterpolator : IVeloxInterpolator
    {
        private readonly VeloxPipelineOptions _options;

        /// <summary>
        /// Creates an interpolator with custom machine options.
        /// </summary>
        /// <param name="options">Options for specific machine.</param>
        public VeloxInterpolator(in VeloxPipelineOptions options) => _options = options;

        /// <summary>
        /// Creates an interpolator with default options.
        /// </summary>
        public VeloxInterpolator() : this(VeloxPipelineOptions.Default) { }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IInterpolationContext, allows ref struct
        {
            double minStepLength = _options.MinStepLength;
            double maxLinearStep = _options.MaxLinearStep;

            PlanningResult planning = context.PlanningResult;
            int blocksCount = planning.Length;
            
            if (blocksCount == 0)
            {
                context.InterpolationResult = new InterpolationResult(null, 0, null, 0);
                return;
            }

            long estimatedTotalPoints = EstimateTotalPoints(in planning, minStepLength, maxLinearStep);
            long finalCapacity = (long)(estimatedTotalPoints * 1.15) + 128;
            int targetPointsCapacity = (int)Math.Min(50_000_000, finalCapacity);

            double lastX = 0.0, lastY = 0.0, lastZ = 0.0;
            double totalTime = 0.0;
            double vPrev = 0.0;

            var pointsList = new ArenaList<TrajectoryPoint>(ref context.Arena, targetPointsCapacity);
            var segmentsList = new ArenaList<TrajectorySegment>(ref context.Arena, blocksCount);
            InterpretationResult interpretation = context.InterpretationResult;

            for (int i = 0; i < blocksCount; i++)
            {
                ref readonly PlannedBlock block = ref planning[i];
                ref readonly Frame frame = ref interpretation.GetFrame(block.FrameIndex);
                double length = block.Length;

                if (length < minStepLength || double.IsNaN(length) || double.IsInfinity(length))
                {
                    lastX = frame.X; lastY = frame.Y; lastZ = frame.Z;
                    continue;
                }

                InterpolationBlock interpolationBlock = PrepareInterpolationBlock(
                    in block, in frame, length, lastX, lastY, lastZ,
                    maxLinearStep, _options.ChordalTolerance, minStepLength);

                if (!interpolationBlock.IsValid)
                {
                    lastX = frame.X; lastY = frame.Y; lastZ = frame.Z;
                    continue;
                }

                double cruiseLength = Math.Max(0.0, length - block.AccelLength - block.DecelLength);
                int startIndex = pointsList.Count;
                int steps = Math.Max(1, (int)Math.Ceiling(length / interpolationBlock.StepLength));
                
                if (steps > 10_000_000) steps = 10_000_000;

                double ds = length / steps;
                vPrev = block.VEntry;

                double relU = interpolationBlock.InitialRelU;
                double relV = interpolationBlock.InitialRelV;

                for (int step = 1; step <= steps; step++)
                {
                    double s = step * ds;
                    double vCurrent = CalculateStepVelocity(in block, s, cruiseLength, minStepLength);

                    double x, y, z;
                    if (!interpolationBlock.IsArc)
                    {
                        GenerateLinearPoint(in interpolationBlock, s, lastX, lastY, lastZ, out x, out y, out z);
                    }
                    else
                    {
                        GenerateArcPoint(in interpolationBlock, s, ref relU, ref relV, out x, out y, out z);
                    }

                    if (block.BacklashX || block.BacklashY || block.BacklashZ)
                    {
                        ApplyBacklashFilter(in block, s, _options.BacklashX, _options.BacklashY, _options.BacklashZ, lastX, lastY, lastZ, ref x, ref y, ref z);
                    }

                    if (step == steps)
                    {
                        x = frame.X; 
                        y = frame.Y; 
                        z = frame.Z;
                    }

                    double vAvg = (vPrev + vCurrent) * 0.5;
                    double dt = (vAvg > 1e-9) ? ds / vAvg : 0.0;
                    totalTime += dt;
                    vPrev = vCurrent;

                    pointsList.Add(new TrajectoryPoint(x, y, z, vCurrent, totalTime));
                }

                int count = pointsList.Count - startIndex;
                if (count > 0) segmentsList.Add(new TrajectorySegment(block.FrameIndex, startIndex, count));

                lastX = frame.X; lastY = frame.Y; lastZ = frame.Z;
            }

            context.InterpolationResult = new InterpolationResult(
                pointsList.AsPointer(), pointsList.Count,
                segmentsList.AsPointer(), segmentsList.Count
            );
        }

        /// <summary>
        /// Pre-calculates the estimated total number of interpolation points across the entire execution plan.
        /// </summary>
        /// <remarks>
        /// This method provides a fast, zero-allocation heuristic to pre-size memory buffers or arenas 
        /// before running the heavy interpolation loops. It filters out degenerate segments and guarantees 
        /// at least one step for valid short movements.
        /// </remarks>
        /// <param name="planning">The immutable look-ahead planning results containing calculated block segments.</param>
        /// <param name="minStepLength">The lower boundary threshold to skip structurally irrelevant or tiny segments.</param>
        /// <param name="maxLinearStep">The maximum permitted distance step size between two spatial trajectory coordinates.</param>
        /// <returns>The accumulated pessimistic estimate of the total points required to process the complete path.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long EstimateTotalPoints(in PlanningResult planning, double minStepLength, double maxLinearStep)
        {
            long estimatedTotalPoints = 0;
            
            for (int i = 0; i < planning.Length; i++)
            {
                double len = planning[i].Length;
                
                if (len < minStepLength || double.IsNaN(len) || double.IsInfinity(len)) continue;
                
                int estimatedSteps = (int)Math.Ceiling(len / maxLinearStep);
                estimatedTotalPoints += estimatedSteps < 1 ? 1 : estimatedSteps;
            }

            return estimatedTotalPoints;
        }

        /// <summary>
        /// Computes the exact 3D spatial coordinates (X, Y, Z) for a linear interpolation step (G1).
        /// </summary>
        /// <remarks>
        /// Employs a highly optimized branchless ratio-based calculation to determine the next tool position 
        /// relative to the absolute coordinates of the previous interpolation point.
        /// </remarks>
        /// <param name="setup">The read-only configuration context containing computed delta distances and inverse length multipliers.</param>
        /// <param name="s">The current linear displacement distance traveled along the trajectory segment profile.</param>
        /// <param name="lastX">The X-coordinate of the immediately preceding interpolation point.</param>
        /// <param name="lastY">The Y-coordinate of the immediately preceding interpolation point.</param>
        /// <param name="lastZ">The Z-coordinate of the immediately preceding interpolation point.</param>
        /// <param name="x">The calculated target absolute X-coordinate output parameter.</param>
        /// <param name="y">The calculated target absolute Y-coordinate output parameter.</param>
        /// <param name="z">The calculated target absolute Z-coordinate output parameter.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateLinearPoint(
            in InterpolationBlock setup, 
            double s, double lastX, double lastY, double lastZ,
            out double x, out double y, out double z)
        {
            double ratio = s * setup.InvLength;

            x = lastX + setup.DeltaX * ratio;
            y = lastY + setup.DeltaY * ratio;
            z = lastZ + setup.DeltaZ * ratio;
        }

        /// <summary>
        /// Computes the exact 3D spatial coordinates (X, Y, Z) for a helical or circular interpolation step (G2/G3).
        /// </summary>
        /// <remarks>
        /// Utilizes a matrix-free rotational trigonometric step layout via pre-calculated Sine/Cosine modifiers 
        /// to step-rotate the relative spatial plane offsets (U, V) and maps them into raw absolute 3D machine coordinates.
        /// </remarks>
        /// <param name="setup">The read-only configuration context containing pre-computed radial, axis, and plane metadata.</param>
        /// <param name="s">The current arc displacement distance traveled along the trajectory segment profile.</param>
        /// <param name="relU">The mutable relative workspace coordinate along the first major plane axis (mutated locally to track the current rotational offset).</param>
        /// <param name="relV">The mutable relative workspace coordinate along the second major plane axis (mutated locally to track the current rotational offset).</param>
        /// <param name="x">The calculated target absolute X-coordinate output parameter mapped from the selected CNC plane.</param>
        /// <param name="y">The calculated target absolute Y-coordinate output parameter mapped from the selected CNC plane.</param>
        /// <param name="z">The calculated target absolute Z-coordinate output parameter mapped from the selected CNC plane.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateArcPoint(
            in InterpolationBlock setup,
            double s, ref double relU, ref double relV, 
            out double x, out double y, out double z)
        {
            double nextRelU = relU * setup.CosStep - relV * setup.SinStep;
            double nextRelV = relU * setup.SinStep + relV * setup.CosStep;
            
            relU = nextRelU;
            relV = nextRelV;
            
            double currentW = setup.StartW + setup.DeltaW * (s * setup.InvLength);
            PlaneHelper.MapUVWToXYZ(setup.CenterU + relU, setup.CenterV + relV, currentW, setup.Plane, out x, out y, out z);
        }

        /// <summary>
        /// Computes the exact current target velocity profile using smooth S-curve velocity boundaries.
        /// </summary>
        /// <param name="block">The read-only active <see cref="PlannedBlock"/> holding resolved kinematic S-curve profile boundary limits (VEntry, VExit, VCruise).</param>
        /// <param name="s">The current absolute scalar distance parameter traversed along the path profile in millimeters.</param>
        /// <param name="cruiseLength">The pre-calculated constant-velocity uniform motion segment length allocated for this block.</param>
        /// <param name="minStepLength">Minimum length threshold used to validate whether acceleration or deceleration phases are active.</param>
        /// <returns>The calculated instantaneous target feedrate velocity value in millimeters per second mapped to the current position s.</returns>
        /// <remarks>
        /// This method evaluates a 3-zone smooth S-curve profile (Acceleration, Cruise, Deceleration). It uses a cubic hermite 
        /// polynomial blend factor: <c>3*tau^2 - 2*tau^3</c> to smoothly interpolate velocities across the boundary transitions, 
        /// ensuring continuous, jerk-bounded acceleration ramps that prevent mechanical machine frame shocks.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CalculateStepVelocity(in PlannedBlock block, double s, double cruiseLength, double minStepLength)
        {
            if (s <= block.AccelLength && block.AccelLength > minStepLength)
            {
                double tau = s / block.AccelLength;
                return block.VEntry + (block.VCruise - block.VEntry) * (3 * tau * tau - 2 * tau * tau * tau);
            }

            if (s <= block.AccelLength + cruiseLength)
            {
                return block.VCruise;
            }

            if (block.DecelLength > minStepLength)
            {
                double sDecel = s - (block.AccelLength + cruiseLength);
                double tau = 1.0 - (sDecel / block.DecelLength);
                return block.VExit + (block.VCruise - block.VExit) * (3 * tau * tau - 2 * tau * tau * tau);
            }

            return block.VExit;
        }

        /// <summary>
        /// Filters spatial point coordinates to apply smooth, isolated single-axis backlash compensation boundaries.
        /// </summary>
        /// <param name="block">The active read-only <see cref="PlannedBlock"/> defining active directional vectors and backlash states.</param>
        /// <param name="s">The current absolute scalar distance traversed along the block profile in millimeters.</param>
        /// <param name="backlashX">The configured physical X-axis mechanical play distance to compensate in millimeters.</param>
        /// <param name="backlashY">The configured physical Y-axis mechanical play distance to compensate in millimeters.</param>
        /// <param name="backlashZ">The configured physical Z-axis mechanical play distance to compensate in millimeters.</param>
        /// <param name="lastX">The historical baseline position on the X-axis from which the motion segment originates.</param>
        /// <param name="lastY">The historical baseline position on the Y-axis from which the motion segment originates.</param>
        /// <param name="lastZ">The historical baseline position on the Z-axis from which the motion segment originates.</param>
        /// <param name="x">A reference to the mutable target spatial X coordinate to be clamped by the backlash filter bounds.</param>
        /// <param name="y">A reference to the mutable target spatial Y coordinate to be clamped by the backlash filter bounds.</param>
        /// <param name="z">A reference to the mutable target spatial Z coordinate to be clamped by the backlash filter bounds.</param>
        /// <remarks>
        /// This method acts as a kinematic gate. During the initial phases of motion execution, if backlash recovery flags 
        /// are active, it redirects the entire scalar step increment to the reversing axis while locking the remaining axes, 
        /// ensuring smooth belt pretensioning via the S-curve profile.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplyBacklashFilter(
            in PlannedBlock block, double s, double backlashX, double backlashY, double backlashZ,
            double lastX, double lastY, double lastZ, ref double x, ref double y, ref double z)
        {
            if (block.BacklashX && backlashX > 0.0 && s <= backlashX)
            {
                y = lastY;
                z = lastZ;
                return;
            }

            if (block.BacklashY && backlashY > 0.0 && s <= backlashY)
            {
                x = lastX;
                z = lastZ;
                return;
            }

            if (block.BacklashZ && backlashZ > 0.0 && s <= backlashZ)
            {
                x = lastX;
                y = lastY;
                return;
            }
        }

        /// <summary>
        /// Prepares immutable interpolation parameters for a single block.
        /// </summary>
        /// <remarks>
        /// Returned as readonly ref struct to ensure zero-allocation and optimal JIT register allocation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InterpolationBlock PrepareInterpolationBlock(
            in PlannedBlock block, in Frame frame, double length,
            double lastX, double lastY, double lastZ,
            double maxLinearStep, double chordalTolerance, double minStepLength)
        {
            bool isArc = block.MotionMode == 2 || block.MotionMode == 3;

            if (!isArc)
            {
                return new InterpolationBlock(
                    stepLength: Math.Min(maxLinearStep, length),
                    invLength: 1.0 / length,
                    deltaX: frame.X - lastX,
                    deltaY: frame.Y - lastY,
                    deltaZ: frame.Z - lastZ
                );
            }

            PlaneHelper.GetUV(lastX, lastY, lastZ, frame.Plane, out double startU, out double startV);
            PlaneHelper.GetUV(frame.X, frame.Y, frame.Z, frame.Plane, out double endU, out double endV);
            PlaneHelper.GetUVOffset(frame.I, frame.J, frame.K, frame.Plane, out double offsetU, out double offsetV);

            double centerU = startU + offsetU;
            double centerV = startV + offsetV;
            double radius = Math.Sqrt(offsetU * offsetU + offsetV * offsetV);

            if (double.IsNaN(radius) || radius < 1e-9)
            {
                return InterpolationBlock.Default;
            }

            double stepLength = Math.Max(minStepLength, Math.Sqrt(8.0 * radius * chordalTolerance));
            double startAngle = Math.Atan2(-offsetV, -offsetU);
            double endAngle = Math.Atan2(endV - centerV, endU - centerU);
            double sweepAngle = endAngle - startAngle;

            if (block.MotionMode == 2 && sweepAngle > 0) sweepAngle -= 2 * Math.PI;
            else if (block.MotionMode == 3 && sweepAngle < 0) sweepAngle += 2 * Math.PI;

            double arcLength = Math.Abs(radius * sweepAngle);
            double invLength = (arcLength > 1e-9) ? (1.0 / arcLength) : 0.0;

            PlaneHelper.GetW(lastX, lastY, lastZ, frame.Plane, out double startW);
            PlaneHelper.GetW(frame.X, frame.Y, frame.Z, frame.Plane, out double endW);

            int estimatedSteps = Math.Max(1, (int)Math.Ceiling(length / stepLength));
            double stepAngle = sweepAngle / estimatedSteps;

            return new InterpolationBlock(
                stepLength: stepLength,
                invLength: invLength,
                centerU: centerU,
                centerV: centerV,
                startW: startW,
                deltaW: endW - startW,
                cosStep: Math.Cos(stepAngle),
                sinStep: Math.Sin(stepAngle),
                initialRelU: -offsetU,
                initialRelV: -offsetV,
                plane: frame.Plane
            );
        }

        /// <summary>
        /// Immutable, stack-only state for block interpolation setup.
        /// Employs explicit memory layout to overlay linear and arc data, fitting tightly into a CPU cache line.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Pack = 8)]
        private readonly ref struct InterpolationBlock
        {
            [FieldOffset(0)] public readonly bool IsValid;
            [FieldOffset(1)] public readonly bool IsArc;
            [FieldOffset(2)] public readonly byte Plane;
            
            [FieldOffset(8)] public readonly double StepLength;
            [FieldOffset(16)] public readonly double InvLength;

            [FieldOffset(24)] public readonly double DeltaX;
            [FieldOffset(32)] public readonly double DeltaY;
            [FieldOffset(40)] public readonly double DeltaZ;

            [FieldOffset(24)] public readonly double CenterU;
            [FieldOffset(32)] public readonly double CenterV;
            [FieldOffset(40)] public readonly double StartW; 

            [FieldOffset(48)] public readonly double DeltaW;
            [FieldOffset(56)] public readonly double CosStep;
            [FieldOffset(64)] public readonly double SinStep;
            [FieldOffset(72)] public readonly double InitialRelU;
            [FieldOffset(80)] public readonly double InitialRelV;

            /// <summary>
            /// Initializes a new instance of <see cref="InterpolationBlock"/> for interpolation.
            /// </summary>
            public InterpolationBlock(double stepLength, double invLength, double deltaX, double deltaY, double deltaZ)
            {
                IsValid = true;
                IsArc = false;
                StepLength = stepLength;
                InvLength = invLength;
                DeltaX = deltaX;
                DeltaY = deltaY;
                DeltaZ = deltaZ;
            }

            /// <summary>
            /// Initializes a new instance of <see cref="InterpolationBlock"/> for arc interpolation.
            /// </summary>
            public InterpolationBlock(
                double stepLength, double invLength, byte plane,
                double centerU, double centerV, double startW, double deltaW,
                double cosStep, double sinStep, double initialRelU, double initialRelV)
            {
                IsValid = true;
                IsArc = true;
                Plane = plane;
                StepLength = stepLength;
                InvLength = invLength;
                CenterU = centerU;
                CenterV = centerV;
                StartW = startW;
                DeltaW = deltaW;
                CosStep = cosStep;
                SinStep = sinStep;
                InitialRelU = initialRelU;
                InitialRelV = initialRelV;
            }

            /// <summary>
            /// Represents an invalid or default state (all zeros/false).
            /// </summary>
            public static InterpolationBlock Default => new();
        }
    }
}
