// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Core;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Metaplay.Cloud.Application
{
    internal class MetaplayOnLogEventTrackSink : ILogEventSink
    {
        public MetaplayOnLogEventTrackSink() { }

        public void Emit(LogEvent logEvent)
        {
            // Only track errors
            if (logEvent.Level != LogEventLevel.Error)
            {
                return;
            }

            // Get the log source separately
            string logSource = "";
            if (logEvent.Properties.TryGetValue(Constants.SourceContextPropertyName, out LogEventPropertyValue value) &&
                value is ScalarValue sv &&
                sv.Value is string rawValue)
            {
                logSource = rawValue;
            }

            // Get the exception string separately 
            string exceptionStr = "";
            if (logEvent.Properties.TryGetValue("Exception", out LogEventPropertyValue exPropValue) &&
                exPropValue is ScalarValue exSv1 &&
                exSv1.Value is string rawExValue1)
            {
                exceptionStr = rawExValue1;
            }
            if (string.IsNullOrEmpty(exceptionStr) && logEvent.Properties.TryGetValue("Ex", out exPropValue) &&
                exPropValue is ScalarValue exSv2 &&
                exSv2.Value is string rawExValue2)
            {
                exceptionStr = rawExValue2;
            }
            if (string.IsNullOrEmpty(exceptionStr) && logEvent.Properties.TryGetValue("Error", out exPropValue) &&
                exPropValue is ScalarValue exSv3 &&
                exSv3.Value is string rawExValue3)
            {
                exceptionStr = rawExValue3;
            }

            // Get the timestamp separately
            MetaTime timestamp = MetaTime.FromDateTime(logEvent.Timestamp.UtcDateTime);

            // Format the log message correctly
            string logMessage = logEvent.RenderMessage(CultureInfo.InvariantCulture);

            MetaLogger.InvokeLogEventLogged(logEvent.Level, logEvent.Exception ?? new Exception(exceptionStr), logSource, timestamp, logMessage);
        }
    }
}
