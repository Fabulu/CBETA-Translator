using System.Runtime.CompilerServices;

namespace ReadZen.Tests;

internal static class ModuleInit
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        AvaloniaTestInfrastructure.EnsureInitialized();
    }
}
