namespace NG.Velox.Parsing.Data
{
    /// <summary>
    /// Bitmask for G-code modal groups. Prevents conflicting commands in the same block
    /// (e.g., G01 and G02 cannot appear together).
    /// </summary>
    /// <remarks>
    /// <b>ISO 6983 modal groups:</b>
    /// Group 1 (Motion): G00, G01, G02, G03 — mutually exclusive
    /// Group 2 (Plane): G17, G18, G19 — mutually exclusive
    /// Group 3 (Distance): G90, G91 — mutually exclusive
    /// 
    /// <b>Extension point:</b> Add new groups for G20/G21 (units), G93/G94 (feed mode), etc.
    /// Use <c>1 &lt;&lt; N</c> pattern to avoid collisions.
    /// </remarks>
    [Flags]
    internal enum ModalGroupMask : uint
    {
        None = 0,

        /// <summary>
        /// Group 1: Motion commands (G00, G01, G02, G03).
        /// </summary>
        MotionGroup = 1 << 0, // 0000 0001

        /// <summary>
        /// Group 2: Plane selection (G17 = XY, G18 = ZX, G19 = YZ).
        /// </summary>
        PlaneSelectGroup = 1 << 1, // 0000 0010

        /// <summary>
        /// Group 3: Positioning (G90 = absolute, G91 = relative).
        /// </summary>
        DistanceGroup = 1 << 2, // 0000 0100
    }
}
