using System.Runtime.InteropServices;

namespace NG.Velox.Interpretation.Data
{
    /// <summary>
    /// Hardware-oriented metadata frame for CNC machine control.
    /// Serialized to binary and sent via UART/Ethernet to microcontrollers.
    /// </summary>
    /// <remarks>
    /// <b>CRITICAL:</b> Size must be multiple of 8, Pack must match.
    /// Current: 32 bytes (4 bytes flags + 28 bytes padding for future fields).
    /// 
    /// <b>Extension point:</b> To add tool ID, spindle speed, laser power, etc.,
    /// add fields BEFORE the padding. Keep total size at 32 bytes (or update to 40/48).
    /// Example:
    /// <code>
    /// public readonly uint MachineFlags;  // 4 bytes
    /// public readonly ushort ToolId;      // 2 bytes
    /// public readonly double SpindleSpeed;// 8 bytes
    /// // ... pad to 32/40/48 bytes
    /// </code>
    /// 
    /// On the C/C++ side, use <c>#pragma pack(push, 8)</c> to match this layout.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 32)]
    public readonly struct MachineFrame
    {
        /// <summary>
        /// Bitmask of machine state flags (M-codes).
        /// </summary>
        /// <remarks>
        /// Bit 0: M00 (program stop)
        /// Bit 1: M03 (spindle CW)
        /// Bit 2: M04 (spindle CCW)
        /// Bit 3: M07 (mist coolant)
        /// Bit 4: M08 (flood coolant)
        /// Extend as needed for your machine.
        /// </remarks>
        public readonly uint MachineFlags;

        /// <summary>
        /// Creates a new machine frame with specified flags.
        /// </summary>
        /// <param name="machineFlags">Machine state flags.</param>
        public MachineFrame(uint machineFlags)
        {
            MachineFlags = machineFlags;
        }
    }
}