using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
// ReSharper disable InconsistentNaming

namespace Serilog.Sinks.MSBuild
{
    public class MSBuildSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;
        private readonly TaskLoggingHelper _loggingHelper;

        public MSBuildSink(ITask task, IFormatProvider formatProvider = null)
        {
            _formatProvider = formatProvider;
            _loggingHelper = new TaskLoggingHelper(task);
        }

        public void Emit(LogEvent logEvent)
        {
            string message = logEvent.RenderMessage(_formatProvider);
            switch (logEvent.Level)
            {
                case LogEventLevel.Verbose:
                    _loggingHelper.LogMessage(MessageImportance.Low, message, null);
                    break;
                case LogEventLevel.Debug:
                    _loggingHelper.LogMessage(MessageImportance.Normal, message, null);
                    break;
                case LogEventLevel.Information:
                    _loggingHelper.LogMessage(MessageImportance.High, message, null);
                    break;
                case LogEventLevel.Warning:
                    _loggingHelper.LogWarning(message, null);
                    break;
                case LogEventLevel.Error:
                    _loggingHelper.LogError(message, null);
                    break;
                case LogEventLevel.Fatal:
                    _loggingHelper.LogCriticalMessage(null, null, null, null, 0, 0, 0, 0, message, null);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static class MSBuildSinkExtensions
    {
        public static LoggerConfiguration MSBuild(this LoggerSinkConfiguration loggerConfiguration, ITask task,
            IFormatProvider formatProvider = null) =>
            loggerConfiguration.Sink(new MSBuildSink(task, formatProvider));
    }
}
