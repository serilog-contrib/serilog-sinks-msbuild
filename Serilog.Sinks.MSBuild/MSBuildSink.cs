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
using System.Diagnostics.CodeAnalysis;
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
        /// The message's subcategory.
        /// </summary>
        public static readonly string Subcategory = nameof(Subcategory);

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

    /// <summary>
    /// A Serilog sink that redirects events to MSBuild.
    /// </summary>
    public class MSBuildSink : ILogEventSink
    {
        private readonly IFormatProvider? _formatProvider;
        private readonly TaskLoggingHelper _loggingHelper;

        /// <summary>
        /// Creates an <see cref="MSBuildSink"/> from an <see cref="ITask"/>.
        /// </summary>
        /// <param name="task">The <see cref="ITask"/> inside which events are sent.</param>
        /// <param name="formatProvider">Supplies culture-specific
        /// formatting information, or <see langword="null"/>.</param>
        public MSBuildSink(ITask task, IFormatProvider? formatProvider = null)
        {
            if (task is null)
                ThrowArgumentNullException(nameof(task));
            _formatProvider = formatProvider;
            _loggingHelper = task is Task taskConcrete ? taskConcrete.Log : new TaskLoggingHelper(task);
        }

        /// <summary>
        /// Creates an <see cref="MSBuildSink"/> from a <see cref="TaskLoggingHelper"/>.
        /// </summary>
        /// <param name="loggingHelper">The <see cref="TaskLoggingHelper"/> to which events are sent.</param>
        /// <param name="formatProvider">Supplies culture-specific
        /// formatting information, or <see langword="null"/>.</param>
        public MSBuildSink(TaskLoggingHelper loggingHelper, IFormatProvider? formatProvider = null)
        {
            if (loggingHelper is null)
                ThrowArgumentNullException(nameof(loggingHelper));
            _formatProvider = formatProvider;
            _loggingHelper = loggingHelper;
        }

        [DoesNotReturn]
        private static void ThrowArgumentNullException(string paramName) =>
            throw new ArgumentNullException(paramName);

        /// <inheritdoc cref="ILogEventSink.Emit"/>
        public void Emit(LogEvent logEvent)
        {
            object? GetScalarValueOrNull(string key)
            {
                if (!logEvent.Properties.TryGetValue(key, out var value)) return null;
                return value is ScalarValue scalarValue ? scalarValue.Value : null;
            }

            string? GetStringOrNull(string key) =>
                GetScalarValueOrNull(key)?.ToString();

            int GetIntOrZero(string key)
            {
                var scalarValue = GetScalarValueOrNull(key);

                if (scalarValue == null) return 0;
                if (scalarValue is int intValue) return intValue;
                if (int.TryParse(scalarValue.ToString(), out intValue)) return intValue;
                return 0;
            }

            string? subcategory = GetStringOrNull(Subcategory);
            string? code = GetStringOrNull(MessageCode);
            string? helpKeyword = GetStringOrNull(HelpKeyword);
            string? file = GetStringOrNull(File);
            int lineNumber = GetIntOrZero(LineNumber);
            int columnNumber = GetIntOrZero(ColumnNumber);
            int lineEndNumber = GetIntOrZero(EndLineNumber);
            int columnEndNumber = GetIntOrZero(EndColumnNumber);
            string message = logEvent.RenderMessage(_formatProvider);

            if (logEvent.Exception is Exception e)
                message += Environment.NewLine + e.ToString();

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
                case LogEventLevel.Fatal:
                    _loggingHelper.LogError(subcategory, code, helpKeyword, file, lineNumber, columnNumber,
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
    /// Adds the <c>WriteTo.MSBuild()</c> extension methods to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class MSBuildSinkConfigurationExtensions
    {
        /// <summary>
        /// Redirects log events to MSBuild, from <paramref name="task"/>.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="task">The MSBuild <see cref="ITask"/> to log events for.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or <c>null</c>.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>Because this sink redirects messages to another logging system,
        /// it is recommended to allow all event levels to pass through.</remarks>
        public static LoggerConfiguration MSBuild(this LoggerSinkConfiguration sinkConfiguration, ITask task,
            IFormatProvider? formatProvider = null) =>
            sinkConfiguration.Sink(new Sinks.MSBuild.MSBuildSink(task, formatProvider));
        
        /// <summary>
        /// Redirects log events to a <see cref="TaskLoggingHelper"/>.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="loggingHelper">The MSBuild <see cref="TaskLoggingHelper"/> to log events to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or <c>null</c>.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>Because this sink redirects messages to another logging system,
        /// it is recommended to allow all event levels to pass through.</remarks>
        public static LoggerConfiguration MSBuild(this LoggerSinkConfiguration sinkConfiguration,
            TaskLoggingHelper loggingHelper, IFormatProvider? formatProvider = null) =>
            sinkConfiguration.Sink(new Sinks.MSBuild.MSBuildSink(loggingHelper, formatProvider));
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    internal sealed class DoesNotReturnAttribute : Attribute { }
}
