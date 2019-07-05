# A Serilog sink for MSBuild

![License: Apache License 2.0](https://img.shields.io/github/license/teo-tsirpanis/serilog-sinks-msbuild.svg)
[![Nuget package: Serilog.Sinks.MSBuild](https://img.shields.io/nuget/v/Serilog.Sinks.MSBuild.svg)](https://www.nuget.org/packages/Serilog.Sinks.MSBuild/)

## How to use

``` csharp
using Microsoft.Build.Utilities;
using Serilog;

public class MyTask: Task {

    public override bool Execute() {
        using (ILogger _logger = new LoggerConfiguration().WriteTo.MSBuild(this).CreateLogger()) {
            _logger.Information("Hello from my custom Serilog-powered task");
            // [...]
            return true;
        };
    }
}
```

## Important properties

Some Serilog properties are important for this sink, as carry significant information for MSBuild.

They are covered in `Serilog.Sinks.MSBuild.MSBuildMessages`.

## Maintainer(s)

- [@teo-tsirpanis](https://github.com/teo-tsirpanis)
