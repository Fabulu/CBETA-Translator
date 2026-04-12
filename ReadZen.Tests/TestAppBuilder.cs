using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(ReadZen.Tests.TestAppBuilder))]

namespace ReadZen.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<Application>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
