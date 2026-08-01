using System.Runtime.InteropServices;

namespace NG.Velox.Interpretation.Data
{
    /// <summary>
    /// Represents the internal Virtual Machine (VM) state captured for a single, fully interpreted G-code block.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Memory Layout &amp; Alignment:</b> This structure enforces a strict <see cref="LayoutKind.Sequential"/> memory layout 
    /// with an explicit size constraint of 104 bytes and an 8-byte boundary alignment (<c>Pack = 8</c>). 
    /// The <see cref="Plane"/> and other <see cref="byte"/>/<see cref="bool"/> control fields are selectively positioned to occupy 
    /// existing padding gaps between 8-byte primitive types (<see cref="double"/>), ensuring that adding state context 
    /// does not increase the overall memory footprint. This dense design is critical for maintaining memory and cache efficiency 
    /// when managing large-scale trajectory buffers exceeding 100k+ elements.
    /// </para>
    /// <para>
    /// <b>Architectural Intended Use:</b> This is a non-serializable, internal snapshot configuration meant strictly for 
    /// kinematic processing within the interpreter. For binary transmission to physical hardware controllers, reference 
    /// <c>MachineFrame</c>. For telemetry, diagnostic exports, or simulation front-ends, reference <c>SimulationFrame</c>.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 104)]
    internal readonly struct Frame
    {
        /// <summary>
        /// Gets the absolute or incremental destination coordinate along the X-axis.
        /// </summary>
        public readonly double X;

        /// <summary>
        /// Gets the absolute or incremental destination coordinate along the Y-axis.
        /// </summary>
        public readonly double Y;

        /// <summary>
        /// Gets the absolute or incremental destination coordinate along the Z-axis.
        /// </summary>
        public readonly double Z;

        /// <summary>
        /// Gets the arc center offset component parallel to the X-axis (commonly defined by the I-word).
        /// </summary>
        public readonly double I;

        /// <summary>
        /// Gets the arc center offset component parallel to the Y-axis (commonly defined by the J-word).
        /// </summary>
        public readonly double J;

        /// <summary>
        /// Gets the arc center offset component parallel to the Z-axis (commonly defined by the K-word).
        /// </summary>
        public readonly double K;

        /// <summary>
        /// Gets the absolute circle radius parameter for radius-designated helical or circular interpolation (R-word).
        /// </summary>
        public readonly double R;

        /// <summary>
        /// Gets the active working machining plane identifier (e.g., matching indices for G17 XY, G18 ZX, or G19 YZ planes).
        /// </summary>
        public readonly byte Plane;

        /// <summary>
        /// Gets the target linear or rotary translation velocity vector magnitude (F-word specification).
        /// </summary>
        public readonly double FeedRate;

        /// <summary>
        /// Gets the active interpolator group behavior configuration index (e.g., 0 for G00, 1 for G01, 2 for G02, 3 for G03).
        /// </summary>
        public readonly byte MotionMode;

        /// <summary>
        /// Gets a value indicating whether coordinates are interpreted as absolute displacements (<see langword="true"/>, G90) 
        /// or incremental vectors (<see langword="false"/>, G91).
        /// </summary>
        public readonly bool DistanceMode;

        /// <summary>
        /// Gets a value indicating whether this block contains physical coordinate data demanding spatial translation execution.
        /// </summary>
        public readonly bool HasMotion;

        /// <summary>
        /// Gets a value indicating whether valid arc parameters (<see cref="I"/>, <see cref="J"/>, <see cref="K"/>, or <see cref="R"/>) are present for circular execution.
        /// </summary>
        public readonly bool HasArcParams;

        /// <summary>
        /// Initializes a new instance of the <see cref="Frame"/> structure with comprehensive geometric and kinematic state metadata.
        /// </summary>
        /// <param name="x">The destination position on the X-axis.</param>
        /// <param name="y">The destination position on the Y-axis.</param>
        /// <param name="z">The destination position on the Z-axis.</param>
        /// <param name="i">The primary axis circular projection vector component.</param>
        /// <param name="j">The secondary axis circular projection vector component.</param>
        /// <param name="k">The auxiliary depth axis circular projection vector component.</param>
        /// <param name="r">The specific radius override modifier value.</param>
        /// <param name="feedRate">The runtime axis motion constraint profile velocity cap.</param>
        /// <param name="activeMotionMode">The modal selection category representing the active motion interpolation state.</param>
        /// <param name="activePlane">The selected coordinate system projection plane map index.</param>
        /// <param name="isAbsoluteDistance"><see langword="true"/> if operating under absolute coordinate evaluation rules.</param>
        /// <param name="hasMotion"><see langword="true"/> if linear or circular axis displacement is demanded by this block execution.</param>
        /// <param name="hasArcParams"><see langword="true"/> if complex parameters are populated for circular sweep tracking evaluations.</param>
        public Frame(
            double x, double y, double z,
            double i, double j, double k, double r,
            double feedRate,
            byte activeMotionMode, byte activePlane, bool isAbsoluteDistance,
            bool hasMotion, bool hasArcParams)
        {
            X = x; Y = y; Z = z;
            I = i; J = j; K = k; R = r;
            Plane = activePlane;
            FeedRate = feedRate;
            MotionMode = activeMotionMode;
            DistanceMode = isAbsoluteDistance;
            HasMotion = hasMotion;
            HasArcParams = hasArcParams;
        }
    }
}
