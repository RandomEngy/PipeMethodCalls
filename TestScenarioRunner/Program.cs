// See https://aka.ms/new-console-template for more information
using PipeMethodCalls;
using PipeMethodCalls.MessagePack;
using PipeMethodCalls.NetJson;
using TestCore;
using TestScenarioRunner;

const string PipeName = "testpipe";

Scenario? scenario = null;
PipeSide? side = null;
PipeSerializerType? serializerType = null;

for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];

    if (arg.StartsWith("--", StringComparison.Ordinal))
    {
        string argName = arg.Substring(2);
        if (i >= args.Length - 1)
        {
            PrintUsage();
            return 1;
        }

        string argValue = args[i + 1];
        switch (argName)
        {
            case "scenario":
                scenario = Enum.Parse<Scenario>(argValue);
                break;
            case "side":
                side = Enum.Parse<PipeSide>(argValue);
                break;
            case "type":
                serializerType = Enum.Parse<PipeSerializerType>(argValue);
                break;
            default:
                PrintUsage();
                return 1;
        }

        i++;
    }
}

if (scenario == null || side == null || serializerType == null)
{
    PrintUsage();
    return 1;
}

IPipeSerializer pipeSerializer;

switch (serializerType)
{
    case PipeSerializerType.NetJson:
        pipeSerializer = new NetJsonPipeSerializer();
        break;
    case PipeSerializerType.MessagePack:
        pipeSerializer = new MessagePackPipeSerializer();
        break;
    default:
        PrintUsage();
        return 1;
}

switch (scenario)
{
    case Scenario.WithCallback:
        if (side == PipeSide.Server)
        {
            var pipeServerWithCallback = new PipeServerWithCallback<IConcatenator, IAdder>(pipeSerializer, PipeName, () => new Adder());
            pipeServerWithCallback.SetLogger(message => Console.WriteLine(message));
            await WithCallbackScenario.RunServerAsync(pipeServerWithCallback);
        }
        else
        {
            var pipeClientWithCallback = new PipeClientWithCallback<IAdder, IConcatenator>(pipeSerializer, PipeName, () => new Concatenator());
            pipeClientWithCallback.SetLogger(message => Console.WriteLine(message));
            await WithCallbackScenario.RunClientAsync(pipeClientWithCallback);
        }

        break;
    case Scenario.NoCallback:
        if (side == PipeSide.Server)
        {
            var pipeServer = new PipeServer<IAdder>(pipeSerializer, PipeName, () => new Adder());
            pipeServer.SetLogger(message => Console.WriteLine(message));
            await NoCallbackScenario.RunServerAsync(pipeServer);
        }
        else
        {
            var pipeClient = new PipeClient<IAdder>(pipeSerializer, PipeName);
            pipeClient.SetLogger(message => Console.WriteLine(message));
            await NoCallbackScenario.RunClientAsync(pipeClient);
        }

        break;
    default:
        PrintUsage();
        return 1;
}

return 0;

void PrintUsage()
{
    Console.WriteLine("TestScenarioRunner --scenario <scenario> --side <side> --type <type>");
    Console.WriteLine("  --scenario <scenario>");
    Console.WriteLine("    WithCallback, NoCallback");
    Console.WriteLine("  --side <side>");
    Console.WriteLine("    Client, Server");
    Console.WriteLine("  --type <type>");
    Console.WriteLine("    NetJson, MessagePack");
}