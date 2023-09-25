using System.Diagnostics;
using TestCore;

ProcessStartInfo serverInfo = new ProcessStartInfo("TestScenarioRunner", $"--scenario {Scenario.Performance} --side {PipeSide.Server} --type {PipeSerializerType.MessagePack}");
Process serverProcess = Process.Start(serverInfo);

var stopwatch = new Stopwatch();
stopwatch.Start();

ProcessStartInfo clientInfo = new ProcessStartInfo("TestScenarioRunner", $"--scenario {Scenario.Performance} --side {PipeSide.Client} --type {PipeSerializerType.MessagePack}");
Process clientProcess = Process.Start(clientInfo);

clientProcess.WaitForExit();
stopwatch.Stop();

var standardError = new StreamWriter(Console.OpenStandardError());
standardError.AutoFlush = true;

serverProcess.WaitForExit();

//Console.SetError(standardError);

Console.WriteLine();
Console.WriteLine($@"Time elapsed: {stopwatch.Elapsed.TotalMilliseconds} ms");
Console.ReadLine();
