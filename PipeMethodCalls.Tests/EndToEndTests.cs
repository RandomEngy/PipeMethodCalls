using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Diagnostics;
using TestCore;

namespace PipeMethodCalls.Tests
{
    [TestClass]
    public class EndToEndTests
    {
        [TestMethod]
        public void WithCallbacks_NetJson()
        {
            TestScenario(Scenario.WithCallback, PipeSerializerType.NetJson);
        }

        [TestMethod]
        public void WithCallbacks_MessagePack()
        {
            TestScenario(Scenario.WithCallback, PipeSerializerType.MessagePack);
        }

        [TestMethod]
        public void NoCallbacks_NetJson()
        {
            TestScenario(Scenario.NoCallback, PipeSerializerType.NetJson);
        }

        [TestMethod]
        public void NoCallbacks_MessagePack()
        {
            TestScenario(Scenario.NoCallback, PipeSerializerType.MessagePack);
        }

        private static void TestScenario(Scenario scenario, PipeSerializerType pipeSerializerType)
        {
            ProcessStartInfo serverInfo = new ProcessStartInfo("TestScenarioRunner", $"--scenario {scenario} --side {PipeSide.Server} --type {pipeSerializerType}");
            Process serverProcess = Process.Start(serverInfo);

            ProcessStartInfo clientInfo = new ProcessStartInfo("TestScenarioRunner", $"--scenario {scenario} --side {PipeSide.Client} --type {pipeSerializerType}");
            Process clientProcess = Process.Start(clientInfo);

            clientProcess.WaitForExit();
            serverProcess.WaitForExit();

            clientProcess.ExitCode.ShouldBe(0);
            serverProcess.ExitCode.ShouldBe(0);
        }
    }
}