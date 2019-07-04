# A Serilog sink for MSBuild

## How to use

``` csharp
using Microsoft.Build.Utilities;
using Serilog;

public class MyTask: Task {

    public override bool Execute() {
        using (ILogger _logger = new LoggerConfiguration().WriteTo.MSBuild().CreateLogger()) {
            _logger.Information("Hello from my custom Serilog-powered task");
            // [...]
            return true;
        };
    }
}