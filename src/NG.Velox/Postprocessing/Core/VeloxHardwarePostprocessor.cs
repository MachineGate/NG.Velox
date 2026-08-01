using System.Runtime.CompilerServices;

namespace NG.Velox.Postprocessing.Core
{
    using NG.Velox.Helpers;
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Interpolation.Data;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Memory.Core;
    using NG.Velox.Postprocessing.Data;
    using NG.Velox.Postprocessing.Interfaces;

    /// <summary>
    /// Serializes trajectory blocks to a raw binary byte stream for high-speed hardware transmission using unmanaged pointers.
    /// <para>Output Stream Layout: [MachineFrame (32 bytes)] [PointsCount (4 bytes)] [TrajectoryPoint[] (40 bytes each)] ...</para>
    /// </summary>
    /// <remarks>
    /// <b>Zero-Copy Blitting:</b> Pre-calculates the exact total binary payload size upfront and provisions a contiguous 
    /// byte array inside the <see cref="MemoryArena"/>. Writes structures directly to unmanaged addresses via CPU registers 
    /// and performs fast block blitting via <see cref="Buffer.MemoryCopy"/> for points, completely bypassing heap or stack staging buffers.
    /// <para/>
    /// <b>Binary Protocol (C/C++ Side compatibility):</b> On the embedded hardware or real-time controller side, the receiving buffer 
    /// can be cast directly to <c>(MachineFrame*)ptr</c>, read the immediate integer count, and then cast the offset pointer 
    /// directly to <c>(TrajectoryPoint*)ptr</c>. No serialization or string parsing overhead is introduced.
    /// </remarks>
    internal sealed unsafe class VeloxHardwarePostprocessor : IVeloxPostprocessor<byte>
    {
        /// <summary>
        /// Executes the hardware-specific compilation and binary serialization pass over interpreted states 
        /// and interpolated trajectory points managed within the provided execution context.
        /// </summary>
        /// <typeparam name="TContext">The specific context type holding target segments, target buffers, and states, constrained to zero-allocation structures.</typeparam>
        /// <param name="context">A reference to the mutable <typeparamref name="TContext"/> from which flat trajectory coordinates are extracted and serialized into the target byte payload.</param>
        /// <param name="diagnosticBag">A reference to the localized compilation logger used to watch execution safety states and record early hardware-protocol anomalies.</param>
        /// <remarks>
        /// Leverages stack-allocated header buffers and <see cref="MemoryMarshal.AsBytes{T}(ReadOnlySpan{T})"/> 
        /// within the loop to execute direct, zero-allocation memory blitting of trajectory arrays into the target stream, 
        /// combined with the anti-boxing <see langword="allows ref struct"/> constraint.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IPostprocessingContext<byte>, allows ref struct
        {
            InterpolationResult interpolation = context.InterpolationResult;

            int segmentsLength = interpolation.SegmentsCount;
            if (segmentsLength == 0)
            {
                context.PostprocessingResult = new PostprocessingResult<byte>(null, 0);
                return;
            }

            int machineFrameSize = sizeof(MachineFrame);
            int intSize = sizeof(int);
            int pointSize = sizeof(TrajectoryPoint);

            long totalHeaderBytes = (long)segmentsLength * (machineFrameSize + intSize);
            long totalPointsBytes = (long)interpolation.PointsCount * pointSize;
            long totalRequiredBytes = totalHeaderBytes + totalPointsBytes;

            if (totalRequiredBytes > int.MaxValue)
            {
                throw new OverflowException("The generated binary hardware stream size exceeds the maximum allowed 2 GB limit.");
            }

            int totalSizeInBytes = (int)totalRequiredBytes;

            byte* outputBuffer = ArenaAllocator.Allocate<byte>(ref context.Arena, totalSizeInBytes);
            byte* pWrite = outputBuffer;

            InterpretationResult interpretation = context.InterpretationResult;

            MachineFrame* machineFramesPtr = interpretation.MachineFramesPtr;
            TrajectorySegment* segmentsPtr = interpolation.SegmentsPtr;
            TrajectoryPoint* allPointsPtr = interpolation.PointsPtr;
            
            for (int i = 0; i < segmentsLength; i++)
            {
                ref readonly TrajectorySegment block = ref segmentsPtr[i];

                Unsafe.Write(pWrite, machineFramesPtr[block.FrameIndex]);
                pWrite += machineFrameSize;

                Unsafe.Write(pWrite, block.Count);
                pWrite += intSize;

                if (block.Count > 0)
                {
                    long bytesToCopy = (long)block.Count * pointSize;

                    TrajectoryPoint* pSegmentPointsStart = allPointsPtr + block.StartIndex;

                    Buffer.MemoryCopy(pSegmentPointsStart, pWrite, bytesToCopy, bytesToCopy);

                    pWrite += bytesToCopy;
                }
            }

            context.PostprocessingResult = new PostprocessingResult<byte>(outputBuffer, totalSizeInBytes);
        }
    }
}
