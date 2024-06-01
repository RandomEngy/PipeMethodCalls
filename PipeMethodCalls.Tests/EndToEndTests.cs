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

		[TestMethod]
		public void ServerCrash()
		{
			Process serverProcess = StartRunnerProcess(Scenario.ServerCrash, PipeSerializerType.NetJson, PipeSide.Server);
			Process clientProcess = StartRunnerProcess(Scenario.ServerCrash, PipeSerializerType.NetJson, PipeSide.Client);

			clientProcess.WaitForExit();
			serverProcess.WaitForExit();

			clientProcess.ExitCode.ShouldBe(0);
			serverProcess.ExitCode.ShouldBe(1);
		}

		private static void TestScenario(Scenario scenario, PipeSerializerType pipeSerializerType)
        {
			Process serverProcess = StartRunnerProcess(scenario, pipeSerializerType, PipeSide.Server);
			Process clientProcess = StartRunnerProcess(scenario, pipeSerializerType, PipeSide.Client);

            clientProcess.WaitForExit();
            serverProcess.WaitForExit();

            clientProcess.ExitCode.ShouldBe(0);
            serverProcess.ExitCode.ShouldBe(0);
        }

		private static Process StartRunnerProcess(Scenario scenario, PipeSerializerType pipeSerializerType, PipeSide side)
		{
			ProcessStartInfo processInfo = new ProcessStartInfo("TestScenarioRunner", $"--scenario {scenario} --side {side} --type {pipeSerializerType}");
			return Process.Start(processInfo);
		}
    }
}