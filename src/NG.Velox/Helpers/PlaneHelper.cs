using NG.Velox.Interpretation.Data;
using NG.Velox.Planning.Data;
using System.Runtime.CompilerServices;

namespace NG.Velox.Helpers
{
    /// <summary>
    /// Helper for 3D plane operations (G17/G18/G19) using UVW normalization.
    /// Reduces 3D arc math to 2D by mapping any plane to canonical UV coordinates.
    /// </summary>
    /// <remarks>
    /// <b>Why UVW?</b> Instead of writing 3 separate math paths for XY/ZX/YZ planes,
    /// we map to UV (2D plane of the arc) + W (perpendicular axis), do all math in 2D,
    /// then map back. This eliminates bugs and makes the code ~3x shorter.
    /// 
    /// <b>Extension point:</b> To add tilted planes (G68.2) or 4th-axis interpolation,
    /// extend the switch statements in GetUV/GetW/MapUVWToXYZ.
    /// </remarks>
    internal static class PlaneHelper
    {
        /// <summary>
        /// Maps IJK offsets to UV offsets based on the active plane.
        /// G17 (XY): U=I, V=J
        /// G18 (ZX): U=K, V=I
        /// G19 (YZ): U=J, V=K
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUVOffset(double i, double j, double k, byte plane, out double u, out double v)
        {
            switch (plane)
            {
                case 18: u = k; v = i; break; // G18: ZX
                case 19: u = j; v = k; break; // G19: YZ
                default: u = i; v = j; break; // G17: XY
            }
        }

        /// <summary>
        /// Maps UV offsets back to IJK offsets based on the active plane.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetIJKFromUV(double u, double v, byte plane, out double i, out double j, out double k)
        {
            i = j = k = 0.0;
            switch (plane)
            {
                case 18: k = u; i = v; break;
                case 19: j = u; k = v; break;
                default: i = u; j = v; break;
            }
        }

        /// <summary>
        /// Calculates the arc center offsets (I, J, K) from the radius (R), start/end points, and motion mode.
        /// Uses UVW normalization to handle any plane (G17/G18/G19) with a single math path.
        /// </summary>
        public static void CalculateArcCenterFromR(
            double startX, double startY, double startZ,
            double endX, double endY, double endZ,
            double r, byte motionMode, byte plane,
            out double i, out double j, out double k)
        {
            GetUV(startX, startY, startZ, plane, out double uStart, out double vStart);
            GetUV(endX, endY, endZ, plane, out double uEnd, out double vEnd);

            double du = uEnd - uStart;
            double dv = vEnd - vStart;
            double distSq = du * du + dv * dv;
            double dist = Math.Sqrt(distSq);

            if (dist < 1e-9 || Math.Abs(r) < 1e-9)
            {
                i = j = k = 0.0;
                return;
            }

            double hSq = r * r - distSq / 4.0;
            
            if (hSq < 0) hSq = 0;
            
            double h = Math.Sqrt(hSq);
            double hSigned = (r > 0) ? h : -h;

            double nu, nv;
            if (motionMode == 3) // G03 CCW
            {
                nu = -dv / dist;
                nv = du / dist;
            }
            else // G02 CW
            {
                nu = dv / dist;
                nv = -du / dist;
            }

            double mu = uStart + du / 2.0;
            double mv = vStart + dv / 2.0;
            double uCenter = mu + hSigned * nu;
            double vCenter = mv + hSigned * nv;

            double uOffset = uCenter - uStart;
            double vOffset = vCenter - vStart;

            GetIJKFromUV(uOffset, vOffset, plane, out i, out j, out k);
        }

        /// <summary>
        /// Maps XYZ coordinates to UV coordinates based on the active plane.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUV(double x, double y, double z, byte plane, out double u, out double v)
        {
            switch (plane)
            {
                case 18: u = z; v = x; break;
                case 19: u = y; v = z; break;
                default: u = x; v = y; break;
            }
        }

        /// <summary>
        /// Extracts the W coordinate (the axis perpendicular to the UV plane) based on the active plane.
        /// G17 (XY): W = Z
        /// G18 (ZX): W = Y
        /// G19 (YZ): W = X
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetW(double x, double y, double z, byte plane, out double w)
        {
            switch (plane)
            {
                case 18: w = y; break; // G18 (ZX plane) -> W is Y
                case 19: w = x; break; // G19 (YZ plane) -> W is X
                default: w = z; break; // G17 (XY plane) -> W is Z
            }
        }

        /// <summary>
        /// Maps UVW coordinates back to standard XYZ 3D coordinates based on the active plane.
        /// This is the inverse operation of GetUV and GetW.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MapUVWToXYZ(double u, double v, double w, byte plane, out double x, out double y, out double z)
        {
            switch (plane)
            {
                case 18: // G18: U=Z, V=X, W=Y
                    x = v;
                    y = w;
                    z = u;
                    break;
                case 19: // G19: U=Y, V=Z, W=X
                    x = w;
                    y = u;
                    z = v;
                    break;
                default: // G17: U=X, V=Y, W=Z
                    x = u;
                    y = v;
                    z = w;
                    break;
            }
        }

        /// <summary>
        /// Directly updates the directional components by reference based on the specified plane orientation code.
        /// Optimized for high-performance operations by eliminating memory allocations and inline expansion.
        /// </summary>
        /// <param name="plane">The plane orientation identifier (e.g., 18 for V-0-U, 19 for 0-U-V, default for U-V-0).</param>
        /// <param name="normTanU">The normalized tangent value along the U axis.</param>
        /// <param name="normTanV">The normalized tangent value along the V axis.</param>
        /// <param name="dirX">A reference to the X directional component to be updated.</param>
        /// <param name="dirY">A reference to the Y directional component to be updated.</param>
        /// <param name="dirZ">A reference to the Z directional component to be updated.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MapDirections(ref PlannedBlock block, byte plane, double normTanU, double normTanV)
        {
            switch (plane)
            {
                case 18: block.DirX = normTanV; block.DirY = 0; block.DirZ = normTanU; break;
                case 19: block.DirX = 0; block.DirY = normTanU; block.DirZ = normTanV; break;
                default: block.DirX = normTanU; block.DirY = normTanV; block.DirZ = 0; break;
            }
        }
    }
}
