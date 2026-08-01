namespace NG.Velox.Helpers
{
    using NG.Velox.Planning.Data;

    /// <summary>
    /// Provides high-performance, allocation-free kinematic utility methods for calculating 
    /// geometric and trajectory profiles of motion control blocks.
    /// </summary>
    /// <remarks>
    /// This helper is designed for critical path performance (hot loops), optimizing CPU register 
    /// caching and minimizing memory overhead during velocity and acceleration ramp evaluations.
    /// </remarks>
    internal static class CurveHelper
    {
        /// <summary>
        /// Calculates the cruise velocity, acceleration distance, and deceleration distance for a kinematic block.
        /// </summary>
        /// <param name="block">A reference to the mutable PlannedBlock to compute profiles for.</param>
        /// <param name="maxAcceleration">Maximum acceleration limit (unpacked from options).</param>
        /// <param name="maxJerk">Maximum jerk limit (unpacked from options).</param>
        public static void CalculateCurveProfile(ref PlannedBlock block, double maxAcceleration, double maxJerk)
        {
            double vEntry = block.VEntry;
            double vExit = block.VExit;
            double vNominal = block.NominalSpeed;
            double length = block.Length;

            double vLow = Math.Max(vEntry, vExit);
            double vHigh = vNominal;
            double vPeak = vLow;

            for (var iter = 0; iter < 16; iter++)
            {
                double vMid = (vLow + vHigh) * 0.5;
                double sAccel = CalculateCurveLength(vEntry, vMid, maxAcceleration, maxJerk);
                double sDecel = CalculateCurveLength(vExit, vMid, maxAcceleration, maxJerk);

                if (sAccel + sDecel > length)
                {
                    vHigh = vMid;
                }
                else
                {
                    vPeak = vMid;
                    vLow = vMid;
                }
            }

            block.VCruise = vPeak;
            block.AccelLength = CalculateCurveLength(vEntry, vPeak, maxAcceleration, maxJerk);
            block.DecelLength = CalculateCurveLength(vExit, vPeak, maxAcceleration, maxJerk);
        }

        /// <summary>
        /// Computes the precise linear trajectory length required to transition between two velocity profiles.
        /// </summary>
        /// <param name="v1">The starting velocity.</param>
        /// <param name="v2">The target velocity.</param>
        /// <param name="maxAcceleration">Maximum acceleration limit (passed as parameter for JIT register caching).</param>
        /// <param name="maxJerk">Maximum jerk limit (passed as parameter for JIT register caching).</param>
        /// <returns>The calculated geometric profile distance parameter required to execute the transition.</returns>
        /// <remarks>
        /// If the requested velocity transition delta falls within a negligible range, it returns zero. 
        /// It dynamically evaluates jerk limits to decide between constant acceleration ramps or smooth S-curve profiles.
        /// </remarks>
        public static double CalculateCurveLength(double v1, double v2, double maxAcceleration, double maxJerk)
        {
            double dv = Math.Abs(v2 - v1);
            if (dv <= 1e-4) return 0.0;

            double dvCritical = (maxAcceleration * maxAcceleration) / maxJerk;

            if (dv < dvCritical)
            {
                double tj = Math.Sqrt(dv / maxJerk);
                double sCurveDist = 2.0 * v1 * tj + dv * tj;
                double sLinearDist = (dv * (v1 + v2)) / (2.0 * maxAcceleration);

                return Math.Min(sCurveDist, sLinearDist);
            }
            else
            {
                double tj = maxAcceleration / maxJerk;
                double ta = (dv - dvCritical) / maxAcceleration;

                double t = ta + 2.0 * tj;
                return t * (v1 + v2) * 0.5;
            }
        }
    }
}
