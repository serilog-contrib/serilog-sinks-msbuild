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
}

namespace Serilog
{
    public static class MSBuildSinkConfigurationExtensions
    {
        public static LoggerConfiguration MSBuild(this LoggerSinkConfiguration loggerConfiguration, ITask task,
            IFormatProvider formatProvider = null) =>
            loggerConfiguration.Sink(new Serilog.Sinks.MSBuild.MSBuildSink(task, formatProvider));
    }
}