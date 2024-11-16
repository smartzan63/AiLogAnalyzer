using AiLogAnalyzer.Core.Configuration;

namespace Integration.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.Tests;

[TestClass]
public static class TestAssemblyInitializer
{
    public static AppConfig AppConfig { get; private set; }

    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        AppConfig = TestConfiguration.GetAppConfig();
    }
}