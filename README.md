# PipeMethodCalls
Lightweight library for method calls over named and anonymous pipes for IPC in .NET Core. Supports two-way communication with callbacks.

### Calls from client to server

```csharp
var pipeServer = new PipeServer<IAdder>(
    new NetJsonPipeSerializer(),
    "mypipe",
    () => new Adder());
await pipeServer.WaitForConnectionAsync();
```

```csharp
var pipeClient = new PipeClient<IAdder>(new NetJsonPipeSerializer(), "mypipe");
await pipeClient.ConnectAsync();
int result = await pipeClient.InvokeAsync(adder => adder.AddNumbers(1, 3));
```

### Calls both way

```csharp
var pipeServer = new PipeServerWithCallback<IConcatenator, IAdder>(
    new NetJsonPipeSerializer(),
    "mypipe",
    () => new Adder());
await pipeServer.WaitForConnectionAsync();
string concatResult = await pipeServer.InvokeAsync(c => c.Concatenate("a", "b"));
```

```csharp
var pipeClient = new PipeClientWithCallback<IAdder, IConcatenator>(
    new NetJsonPipeSerializer(),
    "mypipe",
    () => new Concatenator());
await pipeClient.ConnectAsync();
int result = await pipeClient.InvokeAsync(a => a.AddNumbers(4, 7));
```

### About the library
This library uses named pipes to invoke method calls on a remote endpoint. The method arguments are serialized to binary and sent over the pipe.

### Serialization
PipeMethodCalls supports customizable serialization logic through `IPipeSerializer`. You've got two options:

* Use the pre-built serializer `new NetJsonPipeSerializer()` from the PipeMethodCalls.NetJson package. That uses the System.Text.Json serializer.
* Plug in your own implementation of `IPipeSerializer`. Refer to [the NetJsonPipeSerializer code](https://github.com/RandomEngy/PipeMethodCalls/blob/master/PipeMethodCalls.NetJson/NetJsonPipeSerializer.cs) for an example of how to do this. This method also supports binary serializers like MessagePack.

Open an issue or pull request if you'd like to see more built-in serializers.

### Features
* 100% asynchronous communication with .ConfigureAwait(false) to minimize context switches and reduce thread use
* 45KB with no built-in dependencies
* Invoking async methods
* Passing and returning complex types with pluggable JSON or binary serialization
* Interleaved or multiple simultaneous calls
* Throwing exceptions
* CancellationToken support
* Works on Windows, Linux and MacOS

### Not supported
* Methods with out and ref parameters
* Properties
* Method overloads

