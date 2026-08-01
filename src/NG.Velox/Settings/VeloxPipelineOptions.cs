using System.Runtime.CompilerServices;

namespace NG.Velox.Pipeline.Data
{
    /// <summary>
    /// Configuration options for the Velox CNC pipeline.
    /// Contains kinematic limits and interpolation settings for a specific machine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This immutable struct is passed by value to the Planner and Interpolator.
    /// Default values are tuned for a typical CNC router/mill.
    /// </para>
    /// <para>
    /// Use this to adapt the pipeline to your specific hardware:
    /// <list type="bullet">
    /// <item><description>Desktop DIY machines: lower acceleration/jerk, higher precision.</description></item>
    /// <item><description>Industrial portals: higher acceleration/jerk, wider tolerances.</description></item>
    /// <item><description>3D printers: very low jerk, high chordal tolerance for smooth curves.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // For a desktop CNC router
    /// var options = new VeloxPipelineOptions(
    ///     maxAcceleration: 200.0,    // mm/s²
    ///     maxJerk: 100.0,            // mm/s³
    ///     junctionDeviation: 0.005,  // mm (high precision)
    ///     chordalTolerance: 0.001    // mm (very smooth arcs)
    /// );
    /// var pipeline = VeloxPipelineFactory.CreateSimulation(options);
    /// </code>
    /// </example>
    public readonly struct VeloxPipelineOptions
    {
        #region Planner Settings

        /// <summary>
        /// Maximum acceleration limit for all axes (mm/s²).
        /// Used by the Look-Ahead planner to calculate S-curve profiles.
        /// Typical range: 100 - 5000 mm/s².
        /// </summary>
        public readonly double MaxAcceleration;

        /// <summary>
        /// Maximum jerk limit (mm/s³).
        /// Controls how fast acceleration can change — critical for vibration-free motion.
        /// Lower values = smoother motion but slower cycle times.
        /// Typical range: 50 - 10000 mm/s³.
        /// </summary>
        public readonly double MaxJerk;

        /// <summary>
        /// Tolerance for junction deviation (mm).
        /// Larger values = faster cornering but less precise path following.
        /// Smaller values = precise corners but more deceleration.
        /// Typical range: 0.005 - 0.05 mm.
        /// </summary>
        public readonly double JunctionDeviation;

        /// <summary>
        /// Minimum block length to be considered valid (mm).
        /// Blocks shorter than this are discarded to avoid numerical instability.
        /// </summary>
        public readonly double MinBlockLength;

        #endregion

        #region Backlash Settings

        /// <summary>
        /// Backlash compensation distance for the X-axis (mm).
        /// Amount of extra travel added when the X-axis reverses direction to eliminate mechanical play.
        /// </summary>
        public readonly double BacklashX;

        /// <summary>
        /// Backlash compensation distance for the Y-axis (mm).
        /// Amount of extra travel added when the Y-axis reverses direction to eliminate mechanical play.
        /// </summary>
        public readonly double BacklashY;

        /// <summary>
        /// Backlash compensation distance for the Z-axis (mm).
        /// Amount of extra travel added when the Z-axis reverses direction to eliminate mechanical play.
        /// </summary>
        public readonly double BacklashZ;

        #endregion

        #region Interpolator Settings

        /// <summary>
        /// Chordal tolerance for arc linearization (mm).
        /// Controls how finely arcs are subdivided into line segments.
        /// Smaller values = smoother arcs but more trajectory points (higher memory/CPU usage).
        /// Typical range: 0.001 - 0.01 mm.
        /// </summary>
        public readonly double ChordalTolerance;

        /// <summary>
        /// Maximum step length for linear movements (mm).
        /// Limits the distance between consecutive trajectory points on G00/G01.
        /// </summary>
        public readonly double MaxLinearStep;

        /// <summary>
        /// Minimum step length for any movement (mm).
        /// Prevents zero-length steps that could cause division by zero.
        /// </summary>
        public readonly double MinStepLength;

        #endregion

        /// <summary>
        /// Creates pipeline options with specified values.
        /// </summary>
        /// <param name="maxAcceleration">Maximum acceleration (mm/s²). Must be > 0.</param>
        /// <param name="maxJerk">Maximum jerk (mm/s³). Must be > 0.</param>
        /// <param name="junctionDeviation">Junction deviation tolerance (mm). Must be > 0.</param>
        /// <param name="minBlockLength">Minimum block length (mm). Must be > 0.</param>
        /// <param name="backlashX">Backlash distance for X-axis (mm). Must be >= 0.</param>
        /// <param name="backlashY">Backlash distance for Y-axis (mm). Must be >= 0.</param>
        /// <param name="backlashZ">Backlash distance for Z-axis (mm). Must be >= 0.</param>
        /// <param name="chordalTolerance">Chordal tolerance for arcs (mm). Must be > 0.</param>
        /// <param name="maxLinearStep">Maximum linear step (mm). Must be > 0.</param>
        /// <param name="minStepLength">Minimum step length (mm). Must be > 0 and ≤ maxLinearStep.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a parameter violates strict bounds constraints.</exception>
        /// <exception cref="ArgumentException">Thrown when minStepLength > maxLinearStep.</exception>
        public VeloxPipelineOptions(
            double maxAcceleration = 1000.0,
            double maxJerk = 500.0,
            double junctionDeviation = 0.01,
            double minBlockLength = 0.0001,
            double backlashX = 0.0,
            double backlashY = 0.0,
            double backlashZ = 0.0,
            double chordalTolerance = 0.005,
            double maxLinearStep = 0.1,
            double minStepLength = 0.001)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAcceleration);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxJerk);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(junctionDeviation);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minBlockLength);

            ArgumentOutOfRangeException.ThrowIfNegative(backlashX);
            ArgumentOutOfRangeException.ThrowIfNegative(backlashY);
            ArgumentOutOfRangeException.ThrowIfNegative(backlashZ);

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chordalTolerance);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLinearStep);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minStepLength);

            if (minStepLength > maxLinearStep) throw new ArgumentException("MinStepLength must be <= MaxLinearStep");

            MaxAcceleration = maxAcceleration;
            MaxJerk = maxJerk;
            JunctionDeviation = junctionDeviation;
            MinBlockLength = minBlockLength;

            BacklashX = backlashX;
            BacklashY = backlashY;
            BacklashZ = backlashZ;

            ChordalTolerance = chordalTolerance;
            MaxLinearStep = maxLinearStep;
            MinStepLength = minStepLength;
        }

        /// <summary>
        /// Default options tuned for a typical CNC router.
        /// </summary>
        public static VeloxPipelineOptions Default
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        } = new VeloxPipelineOptions(1000.0, 500.0, 0.01, 0.0001, 0.0, 0.0, 0.0, 0.005, 0.1, 0.001);
    }
}
