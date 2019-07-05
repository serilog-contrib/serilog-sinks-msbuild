// Copyright 2019 Theodore Tsirpanis
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using static Serilog.Sinks.MSBuild.MSBuildProperties;

// ReSharper disable InconsistentNaming

namespace Serilog.Sinks.MSBuild
{
    /// <summary>
    /// <see cref="LogEvent"/> property names that are significant for <see cref="MSBuildSink"/> and would give MSBuild additional information if specified.
    /// </summary>
    /// <remarks>All are optional.</remarks>
    public static class MSBuildProperties
    {
        /// <summary>
        /// The message's error code.
        /// </summary>
        public static readonly string MessageCode = nameof(MessageCode);
        /// <summary>
        /// The help keyword for the host IDE (can be null).
        /// </summary>
        public static readonly string HelpKeyword = nameof(HelpKeyword);
        /// <summary>
        /// The path to the file causing the message.
        /// </summary>
        public static readonly string File = nameof(File);
        /// <summary>
        /// The line in the file causing the message.
        /// </summary>
        public static readonly string LineNumber = nameof(LineNumber);
        /// <summary>
        /// The column in the file causing the message.
        /// </summary>
        public static readonly string ColumnNumber = nameof(ColumnNumber);
        /// <summary>
        /// The last line of a range of lines in the file causing the message,
        /// </summary>
        public static readonly string EndLineNumber = nameof(EndLineNumber);
        /// <summary>
        /// The last column of a range of columns in the file causing the message,
        /// </summary>
        public static readonly string EndColumnNumber = nameof(EndColumnNumber);
    }

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
            string GetPropertyOrNull(string key) =>
                logEvent.Properties.TryGetValue(key, out var value) ? value.ToString() : null;

            int GetIntOrZero(string key) =>
                logEvent.Properties.TryGetValue(key, out var value) && value is ScalarValue scalar &&
                scalar.Value is int numValue
                    ? numValue
                    : 0;

            string subcategory = logEvent.MessageTemplate.Text;
            string code = GetPropertyOrNull(MessageCode);
            string helpKeyword = GetPropertyOrNull(HelpKeyword);
            string file = GetPropertyOrNull(File);
            int lineNumber = GetIntOrZero(LineNumber);
            int columnNumber = GetIntOrZero(ColumnNumber);
            int lineEndNumber = GetIntOrZero(EndLineNumber);
            int columnEndNumber = GetIntOrZero(EndColumnNumber);
            string message = logEvent.RenderMessage(_formatProvider);

            switch (logEvent.Level)
            {
                case LogEventLevel.Verbose:
                    _loggingHelper.LogMessage(subcategory, code, helpKeyword, file, lineNumber, columnNumber,
                        lineEndNumber, columnEndNumber, MessageImportance.Low, message);
                    break;
                case LogEventLevel.Debug:
                    _loggingHelper.LogMessage(subcategory, code, helpKeyword, file, lineNumber, columnNumber,
                        lineEndNumber, columnEndNumber, MessageImportance.Normal, message);
                    break;
                case LogEventLevel.Information:
                    _loggingHelper.LogMessage(subcategory, code, helpKeyword, file, lineNumber, columnNumber,
                        lineEndNumber, columnEndNumber, MessageImportance.High, message);
                    break;
                case LogEventLevel.Warning:
                    _loggingHelper.LogWarning(subcategory, code, helpKeyword, file, lineNumber, columnNumber,
                        lineEndNumber, columnEndNumber, message);
                    break;
                case LogEventLevel.Error:
                    _loggingHelper.LogError(subcategory, code, helpKeyword, file, lineNumber, columnNumber,
                        lineEndNumber, columnEndNumber, message);
                    break;
                case LogEventLevel.Fatal:
                    _loggingHelper.LogCriticalMessage(subcategory, code, helpKeyword, file, lineNumber, columnNumber,
                        lineEndNumber, columnEndNumber, message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

namespace Serilog
{
    /// <summary>
    /// Adds the <c>WriteTo.MSBuild()</c> extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class MSBuildSinkConfigurationExtensions
    {
        /// <summary>
        /// Redirects log events to MSBuild, from <paramref name="task"/>.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="task">The MSBuild <see cref="ITask"/> to log events for.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        public static LoggerConfiguration MSBuild(this LoggerSinkConfiguration sinkConfiguration, ITask task,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum, IFormatProvider formatProvider = null, LoggingLevelSwitch levelSwitch = null) =>
            sinkConfiguration.Sink(new Sinks.MSBuild.MSBuildSink(task, formatProvider), restrictedToMinimumLevel, levelSwitch);
    }
}