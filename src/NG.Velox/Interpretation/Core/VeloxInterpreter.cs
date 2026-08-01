using System.Runtime.CompilerServices;

namespace NG.Velox.Interpretation.Core
{
    using NG.Velox.Helpers;
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Diagnostic.Data;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Interpretation.Interfaces;
    using NG.Velox.Parsing.Data;
    using NG.Velox.Preprocessing.Data;

    /// <summary>
    /// Emulates a CNC virtual machine: tracks coordinates, feeds, motion modes,
    /// planes, and machine state (M-codes) utilizing high-performance raw unmanaged pointer arithmetic.
    /// Produces strictly aligned parallel <see cref="Frame"/> and <see cref="MachineFrame"/> datasets layout sequences.
    /// </summary>
    /// <remarks>
    /// <b>Extension points:</b>
    /// <list type="bullet">
    /// <item><b>New G-codes:</b> Add case in <c>switch (gCode)</c> inside CommandKind.G branch.</item>
    /// <item><b>New M-codes:</b> Add case in <c>switch (mCode)</c> and set bit in <c>machineFlags</c>.</item>
    /// <item><b>New parameters:</b> Add case in <c>switch (node.ParameterKind)</c>.</item>
    /// </list>
    /// <para/>
    /// <b>R→I/J/K conversion:</b> When arc is specified with R (radius) instead of I/J/K,
    /// we calculate arc center via <see cref="PlaneHelper.CalculateArcCenterFromR"/>
    /// before adding the Frame. This keeps Planner/Interpolator simple (they only know I/J/K).
    /// </remarks>
    internal sealed unsafe class VeloxInterpreter : IVeloxInterpreter
    {
        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IInterpretationContext, allows ref struct
        {
            ParsingResult parsing = context.ParsingResult;

            int nodesCount = parsing.Length;
            if (nodesCount == 0)
            {
                context.InterpretationResult = new InterpretationResult(null, null, 0);
                return;
            }

            PreprocessingResult preprocessing = context.PreprocessingResult;

            char* textPtr = preprocessing.TextPtr;
            int* indexMapPtr = preprocessing.IndexMapPtr;
            int textLength = preprocessing.Length;

            Frame* framesBuffer = ArenaAllocator.Allocate<Frame>(ref context.Arena, nodesCount);
            MachineFrame* machineFramesBuffer = ArenaAllocator.Allocate<MachineFrame>(ref context.Arena, nodesCount);
            int frameCount = 0;

            double currentX = 0.0, currentY = 0.0, currentZ = 0.0;
            double lastX = 0.0, lastY = 0.0, lastZ = 0.0;

            double currentFeed = 0.0;
            byte activeMotionMode = 0;
            byte activePlane = 17;
            bool isAbsoluteDistance = true;

            double frameI = 0.0, frameJ = 0.0, frameK = 0.0, frameR = 0.0;
            bool hasMotion = false;
            bool hasArcParams = false;
            uint machineFlags = 0;
            ModalGroupMask usedGroupsInFrame = ModalGroupMask.None;

            int currentTextIndex = 0;
            int currentRow = 1;
            int currentFrameRow = 1;

            for (int i = 0; i < nodesCount; i++)
            {
                ref readonly Node node = ref parsing[i];

                while (currentTextIndex < node.Start && currentTextIndex < textLength)
                {
                    if (textPtr[currentTextIndex].Is(CharMask.NewLine)) currentRow++;
                    currentTextIndex++;
                }

                int nodeRow = currentRow;

                if (nodeRow != currentFrameRow)
                {
                    if (hasArcParams && frameR != 0.0 && frameI == 0.0 && frameJ == 0.0 && frameK == 0.0)
                    {
                        PlaneHelper.CalculateArcCenterFromR(
                            lastX, lastY, lastZ,
                            currentX, currentY, currentZ,
                            frameR, activeMotionMode, activePlane,
                            out frameI, out frameJ, out frameK);
                    }

                    if (hasMotion && (activeMotionMode == 2 || activeMotionMode == 3) && !hasArcParams)
                    {
                        int originalIndex = indexMapPtr[node.Start];
                        diagnosticBag.Add(new Diagnostic(
                            code: 302,
                            start: originalIndex,
                            length: node.Length,
                            severity: Severity.Error
                        ));
                    }

                    framesBuffer[frameCount] = new Frame(
                        currentX, currentY, currentZ,
                        frameI, frameJ, frameK, frameR,
                        currentFeed,
                        activeMotionMode, activePlane, isAbsoluteDistance,
                        hasMotion, hasArcParams);

                    machineFramesBuffer[frameCount] = new MachineFrame(machineFlags);
                    frameCount++;

                    lastX = currentX; lastY = currentY; lastZ = currentZ;
                    frameI = 0.0; frameJ = 0.0; frameK = 0.0; frameR = 0.0;
                    hasMotion = false;
                    hasArcParams = false;
                    usedGroupsInFrame = ModalGroupMask.None;
                    machineFlags = 0;
                    currentFrameRow = nodeRow;
                }

                switch (node.Kind)
                {
                    case NodeKind.Coordinate:
                        hasMotion = true;
                        switch (node.CoordinateKind)
                        {
                            case CoordinateKind.X: currentX = node.Value; break;
                            case CoordinateKind.Y: currentY = node.Value; break;
                            case CoordinateKind.Z: currentZ = node.Value; break;
                        }
                        break;

                    case NodeKind.Parameter:
                        switch (node.ParameterKind)
                        {
                            case ParameterKind.F: currentFeed = node.Value; break;
                            case ParameterKind.I: frameI = node.Value; hasArcParams = true; break;
                            case ParameterKind.J: frameJ = node.Value; hasArcParams = true; break;
                            case ParameterKind.K: frameK = node.Value; hasArcParams = true; break;
                            case ParameterKind.R: frameR = node.Value; hasArcParams = true; break;
                        }
                        break;

                    case NodeKind.Command:
                        switch (node.CommandKind)
                        {
                            case CommandKind.G:
                                int gCode = (int)node.Value;
                                ModalGroupMask gGroup = GetModalGroup(gCode);
                                if (gGroup != ModalGroupMask.None)
                                {
                                    if ((usedGroupsInFrame & gGroup) != 0)
                                    {
                                        int originalIndex = indexMapPtr[node.Start];
                                        diagnosticBag.Add(new Diagnostic(
                                            code: 301,
                                            start: originalIndex,
                                            length: node.Length,
                                            severity: Severity.Error
                                        ));
                                        continue;
                                    }
                                    usedGroupsInFrame |= gGroup;
                                }

                                switch (gCode)
                                {
                                    case 0:
                                    case 1:
                                    case 2:
                                    case 3: activeMotionMode = (byte)gCode; break;
                                    case 17: activePlane = 17; break;
                                    case 18: activePlane = 18; break;
                                    case 19: activePlane = 19; break;
                                    case 90: isAbsoluteDistance = true; break;
                                    case 91: isAbsoluteDistance = false; break;
                                    default:
                                        int originalIndex = indexMapPtr[node.Start];
                                        diagnosticBag.Add(new Diagnostic(
                                            code: 303,
                                            start: originalIndex,
                                            length: node.Length,
                                            severity: Severity.Error
                                        ));
                                        break;
                                }
                                break;

                            case CommandKind.M:
                                int mCode = (int)node.Value;
                                ModalGroupMask mGroup = GetModalGroup(mCode);
                                if (mGroup != ModalGroupMask.None)
                                {
                                    if ((usedGroupsInFrame & mGroup) != 0)
                                    {
                                        int originalIndex = indexMapPtr[node.Start];
                                        diagnosticBag.Add(new Diagnostic(
                                            code: 301,
                                            start: originalIndex,
                                            length: node.Length,
                                            severity: Severity.Error
                                        ));
                                        continue;
                                    }
                                    usedGroupsInFrame |= mGroup;
                                }
                                switch (mCode)
                                {
                                    case 0: machineFlags |= (uint)(1 << 0); break;
                                    default:
                                        int originalIndex = indexMapPtr[node.Start];
                                        diagnosticBag.Add(new Diagnostic(
                                            code: 304,
                                            start: originalIndex,
                                            length: node.Length,
                                            severity: Severity.Error
                                        ));
                                        break;
                                }
                                break;
                        }
                        break;
                }
            }

            if (hasArcParams && frameR != 0.0 && frameI == 0.0 && frameJ == 0.0 && frameK == 0.0)
            {
                PlaneHelper.CalculateArcCenterFromR(
                    lastX, lastY, lastZ,
                    currentX, currentY, currentZ,
                    frameR, activeMotionMode, activePlane,
                    out frameI, out frameJ, out frameK);
            }

            framesBuffer[frameCount] = new Frame(
                currentX, currentY, currentZ,
                frameI, frameJ, frameK, frameR,
                currentFeed,
                activeMotionMode, activePlane, isAbsoluteDistance,
                hasMotion, hasArcParams);

            machineFramesBuffer[frameCount] = new MachineFrame(machineFlags);
            frameCount++;

            context.InterpretationResult = new InterpretationResult(framesBuffer, machineFramesBuffer, frameCount);
        }

        /// <summary>
        /// Identifies the specific G-code modal group associated with a given numerical G-code command.
        /// </summary>
        /// <param name="gCode">The numerical value of the G-code command (e.g., <c>0</c> for G00, <c>17</c> for G17).</param>
        /// <returns>The matching <see cref="ModalGroupMask"/> flag representing the modal group, or <see cref="ModalGroupMask.None"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ModalGroupMask GetModalGroup(int gCode)
        {
            return gCode switch
            {
                0 or 1 or 2 or 3 => ModalGroupMask.MotionGroup,
                17 or 18 or 19 => ModalGroupMask.PlaneSelectGroup,
                90 or 91 => ModalGroupMask.DistanceGroup,
                _ => ModalGroupMask.None
            };
        }
    }
}
