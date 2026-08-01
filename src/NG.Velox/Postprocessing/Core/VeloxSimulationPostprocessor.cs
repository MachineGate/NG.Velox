using System.Runtime.CompilerServices;

namespace NG.Velox.Postprocessing.Core
{
    using NG.Velox.Helpers;
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Interpolation.Data;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Postprocessing.Data;
    using NG.Velox.Postprocessing.Interfaces;

    /// <summary>
    /// Converts internal trajectory data into self-contained unmanaged SimulationFrame tokens.
    /// Runs completely allocation-free on the managed heap by preserving data-locality on the arena.
    /// </summary>
    internal sealed unsafe class VeloxSimulationPostprocessor : IVeloxPostprocessor<SimulationFrame>
    {
        /// <summary>
        /// Materializes flat internal virtual machine states and trajectory points, managed within the provided 
        /// execution context, into structured, standalone simulation objects.
        /// </summary>
        /// <typeparam name="TContext">The specific context type holding simulation payloads and state, constrained to zero-allocation structures.</typeparam>
        /// <param name="context">A reference to the mutable <typeparamref name="TContext"/> containing interpreted configuration frames and globally indexed flat trajectory coordinates used to materialize target entries.</param>
        /// <param name="diagnosticBag">A reference to the localized compilation logger used to watch execution safety states and record early anomalies.</param>
        /// <remarks>
        /// Iterates sequentially through segment indices to map frame boundaries within the context. It converts transient, 
        /// read-only slices of globally tracked trajectory points into independent, heap-allocated arrays before instantiating 
        /// and appending target <see cref="SimulationFrame"/> instances.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IPostprocessingContext<SimulationFrame>, allows ref struct
        {
            InterpolationResult interpolation = context.InterpolationResult;

            int segmentsLength = interpolation.SegmentsCount;
            if (segmentsLength == 0)
            {
                context.PostprocessingResult = new PostprocessingResult<SimulationFrame>(null, 0);
                return;
            }

            InterpretationResult interpretation = context.InterpretationResult;

            MachineFrame* machineFramesPtr = interpretation.MachineFramesPtr;
            Frame* internalFramesPtr = interpretation.FramesPtr;
            TrajectorySegment* segmentsPtr = interpolation.SegmentsPtr;
            TrajectoryPoint* allPointsPtr = interpolation.PointsPtr;

            SimulationFrame* outputBuffer = ArenaAllocator.Allocate<SimulationFrame>(ref context.Arena, segmentsLength);

            for (int i = 0; i < segmentsLength; i++)
            {
                ref readonly TrajectorySegment seg = ref segmentsPtr[i];

                MachineFrame mf = machineFramesPtr[seg.FrameIndex];
                byte motionMode = internalFramesPtr[seg.FrameIndex].MotionMode;

                TrajectoryPoint* segmentPointsPtr = allPointsPtr + seg.StartIndex;

                outputBuffer[i] = new SimulationFrame(mf, segmentPointsPtr, seg.Count, motionMode);
            }

            context.PostprocessingResult = new PostprocessingResult<SimulationFrame>(outputBuffer, segmentsLength);
        }
    }
}
