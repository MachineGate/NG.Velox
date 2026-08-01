using System.Runtime.CompilerServices;

namespace NG.Velox.Planning.Core
{
    using NG.Velox.Helpers;
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Pipeline.Data;
    using NG.Velox.Planning.Data;
    using NG.Velox.Planning.Interfaces;

    /// <summary>
    /// Look-Ahead motion planner with S-curve (jerk-limited) acceleration profiles using high-performance raw unmanaged pointer arithmetic.
    /// Calculates optimal entry/exit/cruise velocities for each block inside a continuous unmanaged memory array.
    /// </summary>
    /// <remarks>
    /// <b>Algorithm:</b>
    /// <list type="number">
    /// <item>Convert Frames to PlannedBlocks (geometry + nominal speed).</item>
    /// <item>Calculate junction speeds using Junction Deviation method.</item>
    /// <item>Backward pass: constrain exit speeds based on next block's entry.</item>
    /// <item>Forward pass: constrain entry speeds based on previous block's exit.</item>
    /// <item>Calculate S-curve profile (accel/cruise/decel lengths).</item>
    /// </list>
    /// <b>Extension point:</b> To add feedrate override, multiply <c>NominalSpeed</c>
    /// by override factor before S-curve calculation.
    /// </remarks>
    internal sealed unsafe class VeloxPlanner : IVeloxPlanner
    {
        private readonly VeloxPipelineOptions _options;

        /// <summary>
        /// Creates a planner with custom machine options.
        /// </summary>
        /// <param name="options">Planner options for specific machine.</param>
        public VeloxPlanner(in VeloxPipelineOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Creates a planner with default options.
        /// </summary>
        public VeloxPlanner() : this(VeloxPipelineOptions.Default) { }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IPlanningContext, allows ref struct
        {
            InterpretationResult interpretation = context.InterpretationResult;

            int framesCount = interpretation.Length;
            if (framesCount == 0)
            {
                context.PlanningResult = new PlanningResult(null, 0);
                return;
            }

            double maxAcceleration = _options.MaxAcceleration;
            double maxJerk = _options.MaxJerk;
            double junctionDeviation = _options.JunctionDeviation;
            double minBlockLength = _options.MinBlockLength;
            double backlashX = _options.BacklashX;
            double backlashY = _options.BacklashY;
            double backlashZ = _options.BacklashZ;

            PlannedBlock* blocksBuffer = ArenaAllocator.Allocate<PlannedBlock>(ref context.Arena, framesCount);

            int blockCount = ConvertFramesToBlocks(
                interpretation, 
                blocksBuffer, 
                minBlockLength, 
                maxAcceleration, 
                maxJerk, 
                junctionDeviation,
                backlashX, 
                backlashY,
                backlashZ
            );

            if (blockCount == 0)
            {
                context.PlanningResult = new PlanningResult(null, 0);
                return;
            }

            var blocks = new Span<PlannedBlock>(blocksBuffer, blockCount);

            // Initialize boundary conditions
            for (int i = 0; i < blockCount; i++)
            {
                ref PlannedBlock block = ref blocks[i];
                block.VEntry = (i == 0) ? 0.0 : block.MaxEntrySpeed;
                block.VExit = (i == blockCount - 1) ? 0.0 : blocks[i + 1].MaxEntrySpeed;
                block.VEntry = Math.Min(block.VEntry, block.NominalSpeed);
                block.VExit = Math.Min(block.VExit, block.NominalSpeed);
            }

            // Backward pass (braking)
            for (int i = blockCount - 1; i > 0; i--)
            {
                ref PlannedBlock newer = ref blocks[i];
                ref PlannedBlock older = ref blocks[i - 1];
                double maxExit = newer.VEntry;
                older.VExit = Math.Min(older.VExit, maxExit);

                if (CurveHelper.CalculateCurveLength(older.VEntry, older.VExit, maxAcceleration, maxJerk) > older.Length)
                {
                    double vLow = older.VExit;
                    double vHigh = older.NominalSpeed;
                    for (int iter = 0; iter < 16; iter++)
                    {
                        double vMid = (vLow + vHigh) * 0.5;
                        if (CurveHelper.CalculateCurveLength(vMid, older.VExit, maxAcceleration, maxJerk) > older.Length) vHigh = vMid;
                        else vLow = vMid;
                    }
                    older.VEntry = vLow;
                }
            }

            // Forward pass (acceleration)
            for (int i = 0; i < blockCount - 1; i++)
            {
                ref PlannedBlock older = ref blocks[i];
                ref PlannedBlock newer = ref blocks[i + 1];
                newer.VEntry = Math.Min(newer.VEntry, older.VExit);

                if (CurveHelper.CalculateCurveLength(newer.VEntry, newer.VExit, maxAcceleration, maxJerk) > newer.Length)
                {
                    double vLow = newer.VEntry;
                    double vHigh = newer.NominalSpeed;
                    for (int iter = 0; iter < 16; iter++)
                    {
                        double vMid = (vLow + vHigh) * 0.5;
                        if (CurveHelper.CalculateCurveLength(newer.VEntry, vMid, maxAcceleration, maxJerk) > newer.Length) vHigh = vMid;
                        else vLow = vMid;
                    }
                    newer.VExit = vLow;
                }
            }

            // Final S-curve profile calculation
            for (int i = 0; i < blockCount; i++)
            {
                ref PlannedBlock block = ref blocks[i];
                CurveHelper.CalculateCurveProfile(ref block, maxAcceleration, maxJerk);
            }

            context.PlanningResult = new PlanningResult(blocksBuffer, blockCount);
        }

        /// <summary>
        /// Converts pure geometric input frames into acceleration-bounded kinematic blocks inside an unmanaged memory buffer.
        /// </summary>
        /// <param name="interpretation">The immutable output containing fully interpreted virtual machine frames and blocks.</param>
        /// <param name="blocksBuffer">The raw unmanaged pointer destination where finalized blocks are stored.</param>
        /// <param name="minBlockLength">Minimum block length threshold (unpacked from options).</param>
        /// <param name="maxAcceleration">Maximum acceleration limit (unpacked from options).</param>
        /// <param name="maxJerk">Maximum jerk limit (unpacked from options).</param>
        /// <param name="junctionDeviation">Junction deviation tolerance (unpacked from options).</param>
        /// <param name="backlashX">Backlash distance for X-axis (unpacked from options).</param>
        /// <param name="backlashY">Backlash distance for Y-axis (unpacked from options).</param>
        /// <param name="backlashZ">Backlash distance for Z-axis (unpacked from options).</param>
        /// <returns>The total number of fully calculated and valid kinematic blocks written to the unmanaged buffer.</returns>
        /// <remarks>
        /// This method acts as a high-speed pipeline dispatcher, sequentially processing geometric steps using raw pointers. 
        /// It filters out numerically unstable short movements, applies dynamic jerk-based limitations for block lengths, 
        /// delegates profile стыковка to boundary calculators, and enforces zero-speed stops for mechanical backlash recovery.
        /// </remarks>
        private static int ConvertFramesToBlocks(
            in InterpretationResult interpretation,
            PlannedBlock* blocksBuffer,
            double minBlockLength,
            double maxAcceleration,
            double maxJerk,
            double junctionDeviation,
            double backlashX,
            double backlashY,
            double backlashZ)
        {
            double lastX = 0.0, lastY = 0.0, lastZ = 0.0;
            double currentFeed = 0.0;
            int blockCount = 0;
            int framesLength = interpretation.Length;

            for (int i = 0; i < framesLength; i++)
            {
                ref readonly Frame frame = ref interpretation.GetFrame(i);
                double currentX = frame.X; double currentY = frame.Y; double currentZ = frame.Z;

                if (frame.FeedRate > 0) currentFeed = frame.FeedRate;
                if (currentFeed <= 0 || !frame.HasMotion)
                {
                    lastX = currentX; lastY = currentY; lastZ = currentZ;
                    continue;
                }

                PlannedBlock block = new()
                {
                    FrameIndex = i,
                    MotionMode = frame.MotionMode,
                    NominalSpeed = currentFeed / 60.0
                };

                if (!ExtractGeometry(ref block, in frame, lastX, lastY, lastZ, currentX, currentY, currentZ, minBlockLength, maxAcceleration))
                {
                    lastX = currentX; lastY = currentY; lastZ = currentZ;
                    continue;
                }

                double jerkSpeedLimit = Math.Pow(maxJerk * block.Length * block.Length, 1.0 / 3.0);
                block.NominalSpeed = Math.Min(block.NominalSpeed, jerkSpeedLimit);

                if (blockCount > 0)
                {
                    ref PlannedBlock prevBlock = ref blocksBuffer[blockCount - 1];

                    CalculateJunctionSpeed(ref block, in prevBlock, in interpretation, maxAcceleration, junctionDeviation);
                    ApplyBacklashCompensation(ref block, in prevBlock, backlashX, backlashY, backlashZ);
                }
                else
                {
                    block.MaxEntrySpeed = 0.0;
                }

                blocksBuffer[blockCount] = block;
                blockCount++;

                lastX = currentX; lastY = currentY; lastZ = currentZ;
            }
            return blockCount;
        }

        /// <summary>
        /// Extracts precise physical path lengths and initial normalized 3D direction vectors from the input motion frame.
        /// </summary>
        /// <param name="block">A reference to the mutable PlannedBlock destination to write resolved geometric data into.</param>
        /// <param name="frame">The read-only immutable source virtual machine frame containing coordinates and plane options.</param>
        /// <param name="lastX">The previous resolved X-axis coordinate parameter used for vector delta computation.</param>
        /// <param name="lastY">The previous resolved Y-axis coordinate parameter used for vector delta computation.</param>
        /// <param name="lastZ">The previous resolved Z-axis coordinate parameter used for vector delta computation.</param>
        /// <param name="currentX">The current target X-axis position extracted from the active pipeline frame segment.</param>
        /// <param name="currentY">The current target Y-axis position extracted from the active pipeline frame segment.</param>
        /// <param name="currentZ">The current target Z-axis position extracted from the active pipeline frame segment.</param>
        /// <param name="minBlockLength">Minimum geometric length threshold required to bypass numerical calculation noise filtering.</param>
        /// <param name="maxAcceleration">Maximum acceleration limit used to enforce physical entry limits for arc trajectories.</param>
        /// <returns><see langword="true"/> if the frame contains valid, executable motion with length exceeding the threshold; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method encapsulates spatial differentiation algorithms, resolving straight line vector deltas or 
        /// calculating complex planar arc projections (G02/G03) using trigonometric radial sweep transformations.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ExtractGeometry(
            ref PlannedBlock block, in Frame frame,
            double lastX, double lastY, double lastZ,
            double currentX, double currentY, double currentZ,
            double minBlockLength, double maxAcceleration)
        {
            if (frame.MotionMode == 0 || frame.MotionMode == 1)
            {
                double dx = currentX - lastX; double dy = currentY - lastY; double dz = currentZ - lastZ;
                double length = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                if (double.IsNaN(length) || double.IsInfinity(length) || length < minBlockLength) return false;

                block.Length = length;
                block.DirX = dx / length; block.DirY = dy / length; block.DirZ = dz / length;
                return true;
            }
            else if (frame.MotionMode == 2 || frame.MotionMode == 3)
            {
                PlaneHelper.GetUV(lastX, lastY, lastZ, frame.Plane, out double lastU, out double lastV);
                PlaneHelper.GetUV(currentX, currentY, currentZ, frame.Plane, out double endU, out double endV);
                PlaneHelper.GetUVOffset(frame.I, frame.J, frame.K, frame.Plane, out double offsetU, out double offsetV);

                double rStart = Math.Sqrt(offsetU * offsetU + offsetV * offsetV);
                double sweepAngle = Math.Atan2(endV - (lastV + offsetV), endU - (lastU + offsetU)) - Math.Atan2(-offsetV, -offsetU);

                if (frame.MotionMode == 2 && sweepAngle > 0) sweepAngle -= 2 * Math.PI;
                else if (frame.MotionMode == 3 && sweepAngle < 0) sweepAngle += 2 * Math.PI;

                double arcLength = Math.Abs(rStart * sweepAngle);
                if (arcLength < minBlockLength) return false;

                block.Length = arcLength;
                if (rStart > 1e-6) block.NominalSpeed = Math.Min(block.NominalSpeed, Math.Sqrt(maxAcceleration * rStart));

                double tanU = frame.MotionMode == 2 ? -offsetV : offsetV;
                double tanV = frame.MotionMode == 2 ? offsetU : -offsetU;
                double tanLen = Math.Sqrt(tanU * tanU + tanV * tanV);

                if (tanLen > minBlockLength) PlaneHelper.MapDirections(ref block, frame.Plane, tanU / tanLen, tanV / tanLen);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Calculates the maximum permissible boundary entry velocity at the junction corner between adjacent blocks.
        /// </summary>
        /// <param name="block">A reference to the active mutable PlannedBlock whose entry speed boundary condition is being resolved.</param>
        /// <param name="prevBlock">The read-only historical PlannedBlock directly preceding the active pipeline segment.</param>
        /// <param name="interpretation">The immutable output layout used to fetch historical source frame coordinates for arc-tangent derivation.</param>
        /// <param name="maxAcceleration">Maximum centripetal acceleration capacity bound allocated for structural frame corners.</param>
        /// <param name="junctionDeviation">Junction deviation tolerance value mapping the geometric circle approximation chord limit.</param>
        /// <remarks>
        /// This method compares the exit vector of the previous segment (accounting for arc rotation delta) against 
        /// the entry vector of the current segment. It computes a precise half-angle cosine transform to dynamically 
        /// scale cornering velocities, matching structural machine capability.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateJunctionSpeed(
            ref PlannedBlock block, in PlannedBlock prevBlock,
            in InterpretationResult interpretation,
            double maxAcceleration, double junctionDeviation)
        {
            double prevExitDirX = prevBlock.DirX;
            double prevExitDirY = prevBlock.DirY;
            double prevExitDirZ = prevBlock.DirZ;

            if (prevBlock.MotionMode == 2 || prevBlock.MotionMode == 3)
            {
                ref readonly Frame prevFrame = ref interpretation.GetFrame(prevBlock.FrameIndex);
                
                PlaneHelper.GetUV(prevFrame.X, prevFrame.Y, prevFrame.Z, prevFrame.Plane, out double endU, out double endV);
                PlaneHelper.GetUVOffset(prevFrame.I, prevFrame.J, prevFrame.K, prevFrame.Plane, out double pOffU, out double pOffV);

                double pLastX = prevBlock.FrameIndex > 0 ? interpretation.GetFrame(prevBlock.FrameIndex - 1).X : 0;
                double pLastY = prevBlock.FrameIndex > 0 ? interpretation.GetFrame(prevBlock.FrameIndex - 1).Y : 0;
                double pLastZ = prevBlock.FrameIndex > 0 ? interpretation.GetFrame(prevBlock.FrameIndex - 1).Z : 0;
                
                PlaneHelper.GetUV(pLastX, pLastY, pLastZ, prevFrame.Plane, out double pLastU, out double pLastV);

                double rEndU = endU - (pLastU + pOffU);
                double rEndV = endV - (pLastV + pOffV);

                double pTanU = prevFrame.MotionMode == 2 ? rEndV : -rEndV;
                double pTanV = prevFrame.MotionMode == 2 ? -rEndU : rEndU;
                double pTanLen = Math.Sqrt(pTanU * pTanU + pTanV * pTanV);

                if (pTanLen > 1e-6)
                {
                    PlannedBlock tempBlock = new();
                    PlaneHelper.MapDirections(ref tempBlock, prevFrame.Plane, pTanU / pTanLen, pTanV / pTanLen);
                    prevExitDirX = tempBlock.DirX; prevExitDirY = tempBlock.DirY; prevExitDirZ = tempBlock.DirZ;
                }
            }

            double cosTheta = prevExitDirX * block.DirX + prevExitDirY * block.DirY + prevExitDirZ * block.DirZ;
            cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta));

            if (cosTheta < 0.9999)
            {
                double cosHalfAlpha = Math.Sqrt(0.5 * (1.0 + cosTheta));
                if (cosHalfAlpha > 0.9999) cosHalfAlpha = 0.9999;

                double junctionSpeed = Math.Sqrt((maxAcceleration * junctionDeviation * cosHalfAlpha) / (1.0 - cosHalfAlpha));
                block.MaxEntrySpeed = Math.Min(block.NominalSpeed, junctionSpeed);
            }
            else
            {
                block.MaxEntrySpeed = block.NominalSpeed;
            }
        }

        /// <summary>
        /// Evaluates multi-axis directional sign changes to inject smooth S-curve backlash compensation boundaries.
        /// </summary>
        /// <param name="block">A reference to the active mutable PlannedBlock where backlash flags and extended distances are applied.</param>
        /// <param name="prevBlock">The read-only historical PlannedBlock used as the directional reference baseline.</param>
        /// <param name="backlashX">The configured physical X-axis mechanical play distance to compensate in millimeters.</param>
        /// <param name="backlashY">The configured physical Y-axis mechanical play distance to compensate in millimeters.</param>
        /// <param name="backlashZ">The configured physical Z-axis mechanical play distance to compensate in millimeters.</param>
        /// <remarks>
        /// This method performs dot product sign evaluations between block direction steps. When an axis reverse is detected, 
        /// it scales the active path length to absorb mechanical play and forces a hard zero-velocity transition, 
        /// preventing motor resonance shocks across elastic belt drives.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplyBacklashCompensation(
            ref PlannedBlock block, in PlannedBlock prevBlock,
            double backlashX, double backlashY, double backlashZ)
        {
            if (backlashX > 0.0 && prevBlock.DirX * block.DirX < -1e-6)
            {
                block.BacklashX = true; 
                block.Length += backlashX; 
                block.MaxEntrySpeed = 0.0;
            }
            if (backlashY > 0.0 && prevBlock.DirY * block.DirY < -1e-6)
            {
                block.BacklashY = true; 
                block.Length += backlashY; 
                block.MaxEntrySpeed = 0.0;
            }
            if (backlashZ > 0.0 && prevBlock.DirZ * block.DirZ < -1e-6)
            {
                block.BacklashZ = true; 
                block.Length += backlashZ; 
                block.MaxEntrySpeed = 0.0;
            }
        }
    }
}
