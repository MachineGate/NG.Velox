using System.Globalization;
using System.Windows.Data;

namespace NG.Velox.Examples
{
    /// <summary>
    /// Converts a <see cref="double"/> value representing total seconds into a formatted time string (HH:MM:SS).
    /// This converter is designed for CNC simulation timelines where the elapsed or remaining duration 
    /// can exceed 24 hours without resetting the hour counter back to zero.
    /// </summary>
    public class DoubleToTimeConverter : IValueConverter
    {
        /// <summary>
        /// Converts a scalar double-precision floating-point number of seconds into a time duration string formatted as HH:MM:SS.
        /// </summary>
        /// <param name="value">The binding source value, expected to be a <see cref="double"/> representing seconds.</param>
        /// <param name="targetType">The type of the binding target property (typically <see cref="string"/>).</param>
        /// <param name="parameter">An optional user-defined parameter to pass to the converter logic.</param>
        /// <param name="culture">The culture info context utilized during string localization formatting phases.</param>
        /// <returns>A formatted time string in HH:MM:SS format, or "00:00:00" if the input value is invalid or null.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double seconds)
            {
                TimeSpan time = TimeSpan.FromSeconds(seconds);
                return $"{((int)time.TotalHours):D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            }
            return "00:00:00";
        }

        /// <summary>
        /// This conversion path is not supported or required by the telemetry viewport.
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown automatically upon any call invocation attempt.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
