using System.Runtime.InteropServices;

namespace NG.Velox.Planning.Data
{
    /// <summary>
    /// Represents an acceleration-bounded kinematic motion block processed by the look-ahead planner.
    /// </summary>
    /// <remarks>
    /// This structure holds the resolved S-curve velocity profile and geometric direction vectors 
    /// required for constant-time path interpolation. It enforces a strict sequential layout with 
    /// 8-byte alignment for cache optimization.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct PlannedBlock
    {
        /// <summary>
        /// Gets or sets the total geometric path length of the motion segment in millimeters.
        /// </summary>
        public double Length;

        /// <summary>
        /// Gets or sets the X-component of the normalized 3D direction vector.
        /// </summary>
        public double DirX;

        /// <summary>
        /// Gets or sets the Y-component of the normalized 3D direction vector.
        /// </summary>
        public double DirY;

        /// <summary>
        /// Gets or sets the Z-component of the normalized 3D direction vector.
        /// </summary>
        public double DirZ;

        /// <summary>
        /// Gets or sets the nominal target feedrate velocity limit in millimeters per second.
        /// </summary>
        public double NominalSpeed;

        /// <summary>
        /// Gets or sets the maximum calculated boundary entry speed permitted at the junction corner.
        /// </summary>
        public double MaxEntrySpeed;

        /// <summary>
        /// Gets or sets the actual finalized entry velocity for the start of the block profile.
        /// </summary>
        public double VEntry;

        /// <summary>
        /// Gets or sets the actual finalized exit velocity for the end of the block profile.
        /// </summary>
        public double VExit;

        /// <summary>
        /// Gets or sets the maximum sustainable cruise velocity achieved during the uniform motion phase.
        /// </summary>
        public double VCruise;

        /// <summary>
        /// Gets or sets the total translation distance required to complete the smooth S-curve acceleration phase.
        /// </summary>
        public double AccelLength;

        /// <summary>
        /// Gets or sets the total translation distance required to complete the smooth S-curve deceleration phase.
        /// </summary>
        public double DecelLength;

        /// <summary>
        /// Gets or sets the zero-based index of the original source virtual machine frame.
        /// </summary>
        public int FrameIndex;

        /// <summary>
        /// Gets or sets the active motion interpolation behavior mode selection category flag.
        /// </summary>
        public byte MotionMode;

        /// <summary>
        /// Gets or sets a value indicating whether smooth X-axis backlash compensation is required at the block entry.
        /// </summary>
        public bool BacklashX;

        /// <summary>
        /// Gets or sets a value indicating whether smooth Y-axis backlash compensation is required at the block entry.
        /// </summary>
        public bool BacklashY;

        /// <summary>
        /// Gets or sets a value indicating whether smooth Z-axis backlash compensation is required at the block entry.
        /// </summary>
        public bool BacklashZ;
    }
}
