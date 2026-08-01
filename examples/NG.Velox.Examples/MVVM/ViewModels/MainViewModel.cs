using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NG.Velox.Examples
{
    using NG.Velox.Pipeline.Extensions;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Factories;
    using NG.Velox.Helpers;
    using NG.Velox.Memory.Core;
    using NG.Velox.Interpolation.Data;
    using NG.Velox.Pipeline.Data;

    /// <summary>
    /// Provides the main ViewModel for the CNC toolpath simulation application.
    /// Manages the real-time playback control loop, debounced asynchronous G-code parsing via NG.Velox,
    /// interpolation of tool positions based on simulation time, and diagnostic error reporting.
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private const string DEFAULT_G_CODE =
            "N001 G00 F1800.000 Z100.000\r\n" +
            "N002 X100.000 Y100.000 Z100.000\r\n" +
            "N003 G03 X120.000 Y80.000 R20.000\r\n" +
            "N004 X100.000 Y60.000 R20.000\r\n" +
            "N005 X80.000 Y80.000 R20.000\r\n" +
            "N006 X100.000 Y100.000 R20.000\r\n" +
            "N007 Y60.000 R20.000\r\n" +
            "N008 X120.000 Y80.000 R20.000\r\n" +
            "N009 X100.000 Y100.000 R20.000\r\n" +
            "N010 G00 X0.000 Y0.000";

        /// <summary>
        /// Initializes a new instance of <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            CodeText = DEFAULT_G_CODE;
        }

        /// <summary>
        /// The cached linear list of interpolated toolpath points resulting from the G-code parsing.
        /// </summary>
        private readonly List<TrajectoryPoint> _toolPath = new();

        /// <summary>
        /// The cancellation token source used to implement the debounce logic for input G-code text modifications.
        /// </summary>
        private CancellationTokenSource? _debounceCts;

        /// <summary>
        /// The high-resolution stopwatch used to measure elapsed real-time during simulation playback.
        /// </summary>
        private readonly Stopwatch _stopwatch = new();

        /// <summary>
        /// The total simulation time accumulated prior to the current active playback session, measured in seconds.
        /// </summary>
        private double _accumulatedTime = 0;

        /// <summary>
        /// Gets or sets the current playback time of the simulation in seconds.
        /// </summary>
        [ObservableProperty]
        public partial double CurrentTime { get; set; }

        /// <summary>
        /// Gets or sets the total execution time duration required by the parsed G-code program in seconds.
        /// </summary>
        [ObservableProperty]
        public partial double TotalTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the simulation is actively playing.
        /// </summary>
        [ObservableProperty]
        public partial bool IsPlaying { get; set; }

        /// <summary>
        /// Gets or sets the UI-displayable text content for the playback execution button (e.g., "Run" or "Pause").
        /// </summary>
        [ObservableProperty]
        public partial string PlayButtonText { get; set; } = "Run";

        /// <summary>
        /// Gets or sets the collection of 3D point coordinates utilized by the HelixToolkit view to render the static toolpath trajectory wires.
        /// </summary>
        [ObservableProperty]
        public partial Point3DCollection TrajectoryPoints { get; set; } = new();

        /// <summary>
        /// Gets or sets the calculated dynamic 3D position vector of the CNC tool head at the current instance of simulation time.
        /// </summary>
        [ObservableProperty]
        public partial Point3D ToolPosition { get; set; }

        /// <summary>
        /// Gets or sets the raw G-code text script inputted from the UI text editor panel.
        /// </summary>
        [ObservableProperty]
        public partial string CodeText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of localized pipeline diagnostic and parsing error log entries visible to the user interface.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<string> ErrorsCollection { get; set; } = new();

        /// <summary>
        /// Gets or sets the maximum allowable axis acceleration rate for look-ahead profile generation.
        /// </summary>
        [ObservableProperty]
        public partial double MaxAcceleration { get; set; } = 400.0;

        /// <summary>
        /// Gets or sets the maximum allowable axis jerk rate constraint limits.
        /// </summary>
        [ObservableProperty]
        public partial double MaxJerk { get; set; } = 12000.0;

        /// <summary>
        /// Gets or sets the look-ahead corner blending junction deviation tolerance limit.
        /// </summary>
        [ObservableProperty]
        public partial double JunctionDeviation { get; set; } = 0.005;

        /// <summary>
        /// Gets or sets the minimal physical geometric length threshold required to process an execution block.
        /// </summary>
        [ObservableProperty]
        public partial double MinBlockLength { get; set; } = 0.0001;

        /// <summary>
        /// Gets or sets the chordal tolerance maximum error allowed when linearizing arc segments.
        /// </summary>
        [ObservableProperty]
        public partial double ChordalTolerance { get; set; } = 0.002;

        /// <summary>
        /// Gets or sets the maximum allowed length step for toolpath geometric subdivision procedures.
        /// </summary>
        [ObservableProperty]
        public partial double MaxLinearStep { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets the absolute minimal linear distance segment length permitted for discrete motion sampling steps.
        /// </summary>
        [ObservableProperty]
        public partial double MinStepLength { get; set; } = 0.001;

        /// <summary>
        /// Invoked automatically whenever the <see cref="CurrentTime"/> property changes.
        /// Recalculates the exact spatial coordinate of the milling tool for the specified time position.
        /// </summary>
        /// <param name="value">The newly assigned simulation timestamp value.</param>
        partial void OnCurrentTimeChanged(double value) => InterpolatePosition(value);

        /// <summary>
        /// Invoked automatically when the <see cref="MaxAcceleration"/> property changes. Forces a pipeline reparse.
        /// </summary>
        /// <param name="value">The updated maximum acceleration rate value.</param>
        partial void OnMaxAccelerationChanged(double value) => TriggerReparse();

        /// <summary>
        /// Invoked automatically when the <see cref="MaxJerk"/> property changes. Forces a pipeline reparse.
        /// </summary>
        /// <param name="value">The updated maximum jerk rate value.</param>
        partial void OnMaxJerkChanged(double value) => TriggerReparse();

        /// <summary>
        /// Invoked automatically when the <see cref="JunctionDeviation"/> property changes. Forces a pipeline reparse.
        /// </summary>
        /// <param name="value">The updated junction deviation value.</param>
        partial void OnJunctionDeviationChanged(double value) => TriggerReparse();

        /// <summary>
        /// Invoked automatically when the <see cref="MinBlockLength"/> property changes. Forces a pipeline reparse.
        /// </summary>
        /// <param name="value">The updated minimal block length threshold.</param>
        partial void OnMinBlockLengthChanged(double value) => TriggerReparse();

        /// <summary>
        /// Invoked automatically when the <see cref="ChordalTolerance"/> property changes. Forces a pipeline reparse.
        /// </summary>
        /// <param name="value">The updated chordal tolerance error limit.</param>
        partial void OnChordalToleranceChanged(double value) => TriggerReparse();

        /// <summary>
        /// Invoked automatically when the <see cref="MaxLinearStep"/> property changes. Forces a pipeline reparse.
        /// </summary>
        /// <param name="value">The updated maximum linear subdivision step size.</param>
        partial void OnMaxLinearStepChanged(double value) => TriggerReparse();

        /// <summary>
        /// Invoked automatically when the <see cref="MinStepLength"/> property changes. Forces a pipeline reparse.
        /// </summary>
        /// <param name="value">The updated minimal step distance length value.</param>
        partial void OnMinStepLengthChanged(double value) => TriggerReparse();

        /// <summary>
        /// Invoked automatically whenever the <see cref="IsPlaying"/> property changes state.
        /// Updates button iconography/text labels and hooks or unhooks the high-frequency <see cref="CompositionTarget.Rendering"/> animation loop event.
        /// </summary>
        /// <param name="value">The target boolean value indicating the playback toggle state.</param>
        partial void OnIsPlayingChanged(bool value)
        {
            PlayButtonText = value ? "Pause" : "Run";

            if (value)
            {
                _accumulatedTime = CurrentTime;
                _stopwatch.Restart();
                CompositionTarget.Rendering += OnRendering;
            }
            else
            {
                _accumulatedTime += _stopwatch.Elapsed.TotalSeconds;
                _stopwatch.Stop();
                CompositionTarget.Rendering -= OnRendering;
            }
        }

        /// <summary>
        /// Handles the framework's per-frame rendering tick callback.
        /// Advances the simulation's progress timestamp relative to the high-precision system stopwatch measurements.
        /// </summary>
        /// <param name="sender">The source sender event origin.</param>
        /// <param name="e">The rendering event data payload parameters.</param>
        private void OnRendering(object? sender, EventArgs e)
        {
            double newTime = _accumulatedTime + _stopwatch.Elapsed.TotalSeconds;

            if (newTime >= TotalTime)
            {
                CurrentTime = TotalTime;
                IsPlaying = false;
            }
            else
            {
                CurrentTime = newTime;
            }
        }

        /// <summary>
        /// Invoked automatically whenever the raw <see cref="CodeText"/> block input gets updated.
        /// Triggers a debouncing mechanism to prevent redundant execution and schedules an asynchronous G-code analysis phase.
        /// </summary>
        /// <param name="value">The updated block of raw G-code text instructions.</param>
        partial void OnCodeTextChanged(string value)
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            _ = DebounceAndParseAsync(value, token);
        }

        /// <summary>
        /// Re-evaluates and triggers the execution of the background parsing workflow 
        /// by simulating a text modification event on the current active G-code string block.
        /// </summary>
        private void TriggerReparse()
        {
            OnCodeTextChanged(CodeText);
        }

        /// <summary>
        /// Delays execution for a specified debounce threshold interval and coordinates background workers to parse text strings safely.
        /// </summary>
        /// <param name="text">The raw text source context representing the complete G-code program input block.</param>
        /// <param name="token">A token instance managing operation cancellation behaviors.</param>
        /// <returns>A structured task representing the asynchronous debounce and parse execution lifecycle.</returns>
        private async Task DebounceAndParseAsync(string text, CancellationToken token)
        {
            try
            {
                await Task.Delay(500, token);
                var (path, errors) = await Task.Run(() => ParseCode(text, token), token);
                ApplyParsedResults(path, errors);
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Processes G-code text processing pipelines inside isolated background tasks utilizing low-allocation <see cref="MemoryArena"/> schemas.
        /// Generates discrete geometric points or extracts corresponding lexer/parser error logs via native unmanaged memory blocks.
        /// </summary>
        /// <param name="text">The source raw string content string containing industrial G-code blocks.</param>
        /// <param name="token">The cancellation token to cleanly stop the pipeline loops if editing occurs mid-parse.</param>
        /// <returns>A tuple struct containing the generated toolpath coordinates sequence and an error description collection.</returns>
        private (List<TrajectoryPoint> Path, List<string> Errors) ParseCode(string text, CancellationToken token)
        {
            var localPath = new List<TrajectoryPoint>();
            var localErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(text)) return (localPath, localErrors);

            var diag = new DiagnosticBag();
            var arena = new MemoryArena(1024 * 1024);

            try
            {
                var pipeline = VeloxPipelineFactory.CreateSimulation(
                    new VeloxPipelineOptions(
                        maxAcceleration: MaxAcceleration,
                        maxJerk: MaxJerk,
                        junctionDeviation: JunctionDeviation,
                        minBlockLength: MinBlockLength,
                        chordalTolerance: ChordalTolerance,
                        maxLinearStep: MaxLinearStep,
                        minStepLength: MinStepLength
                ));

                var input = text.AsMemory();
                var result = pipeline.Process(input, ref arena, ref diag);

                if (!diag.HasErrors)
                {
                    var frames = result.ToArray();
                    foreach (var frame in frames)
                    {
                        token.ThrowIfCancellationRequested();
                        for (int j = 0; j < frame.PointsCount; j++)
                        {
                            unsafe
                            {
                                var point = frame.Points[j];
                                localPath.Add(point);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var diagnostic in diag.Diagnostics)
                    {
                        var (raw, column) = IndexMapper.GetRowColumn(input.Span, diagnostic.Start);
                        localErrors.Add($"Pipeline error {diagnostic.Code} on raw {raw}, column {column}");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                localErrors.Add($"Critical pipeline error: {ex.Message}");
            }
            finally
            {
                arena.Dispose(); diag.Dispose();
            }

            return (localPath, localErrors);
        }

        /// <summary>
        /// Updates the model properties on the primary UI thread with newly derived dataset calculations, resetting active simulation runtime indexes cleanly.
        /// </summary>
        /// <param name="path">The freshly extracted sequential path point coordinate list.</param>
        /// <param name="errors">The corresponding tracking logs describing formatting exceptions or calculation errors.</param>
        private void ApplyParsedResults(List<TrajectoryPoint> path, List<string> errors)
        {
            IsPlaying = false;

            _toolPath.Clear();
            _toolPath.AddRange(path);

            TotalTime = _toolPath.Count > 0 ? _toolPath[^1].TimeStamp : 0;
            CurrentTime = 0;
            _accumulatedTime = 0;
            _stopwatch.Reset();

            var points = new Point3DCollection();
            if (_toolPath.Count > 1)
            {
                for (int i = 0; i < _toolPath.Count - 1; i++)
                {
                    var p1 = _toolPath[i];
                    var p2 = _toolPath[i + 1];
                    points.Add(new Point3D(p1.X, p1.Y, p1.Z));
                    points.Add(new Point3D(p2.X, p2.Y, p2.Z));
                }
            }

            TrajectoryPoints = points;
            InterpolatePosition(0);

            ErrorsCollection.Clear();
            foreach (var err in errors) ErrorsCollection.Add(err);
        }

        /// <summary>
        /// Executes binary search algorithms across sequential toolpaths to compute and linearly interpolate exact 3D coordinates based on runtime durations.
        /// </summary>
        /// <param name="targetTime">The precise current absolute simulation playback timestamp value target in seconds.</param>
        private void InterpolatePosition(double targetTime)
        {
            if (_toolPath.Count == 0)
            {
                ToolPosition = new Point3D(0, 0, 0);
                return;
            }

            if (targetTime <= _toolPath[0].TimeStamp)
            {
                var p = _toolPath[0];
                ToolPosition = new Point3D(p.X, p.Y, p.Z);
                return;
            }

            if (targetTime >= _toolPath[^1].TimeStamp)
            {
                var p = _toolPath[^1];
                ToolPosition = new Point3D(p.X, p.Y, p.Z);
                return;
            }

            int left = 0;
            int right = _toolPath.Count - 1;

            while (left < right - 1)
            {
                int mid = left + (right - left) / 2;
                if (_toolPath[mid].TimeStamp <= targetTime)
                    left = mid;
                else
                    right = mid;
            }

            var p1 = _toolPath[left];
            var p2 = _toolPath[right];

            double duration = p2.TimeStamp - p1.TimeStamp;
            double t = duration > 0 ? (targetTime - p1.TimeStamp) / duration : 0;

            ToolPosition = new Point3D(
                p1.X + (p2.X - p1.X) * t,
                p1.Y + (p2.Y - p1.Y) * t,
                p1.Z + (p2.Z - p1.Z) * t
            );
        }

        /// <summary>
        /// Relays the command invocation to toggle the simulation state between active playback and paused execution modes.
        /// </summary>
        [RelayCommand]
        private void PlayPause() => IsPlaying = !IsPlaying;

        /// <summary>
        /// Relays the command invocation to forcefully pause current processing loops and reset execution clock pointers back to zero indexes.
        /// </summary>
        [RelayCommand]
        private void ResetSimulation()
        {
            IsPlaying = false;
            CurrentTime = 0;
            _accumulatedTime = 0;
            _stopwatch.Reset();
        }
    }
}
