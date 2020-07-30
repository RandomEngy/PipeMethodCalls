# PipeMethodCalls
Lightweight library for method calls over named and anonymous pipes for IPC in .NET Core. Supports two-way communication with callbacks.

### Calls from client to server

```csharp
var pipeServer = new PipeServer<IAdder>("mypipe", () => new Adder());
await pipeServer.WaitForConnectionAsync();
```

```csharp
var pipeClient = new PipeClient<IAdder>("mypipe");
await pipeClient.ConnectAsync();
int result = await pipeClient.InvokeAsync(a => a.AddNumbers(1, 3));
```

### Calls both way

```csharp
var pipeServer = new PipeServerWithCallback<IConcatenator, IAdder>("testpipe", () => new Adder());
await pipeServer.WaitForConnectionAsync();
string concatResult = await pipeServer.InvokeAsync(c => c.Concatenate("a", "b"));
```

```csharp
var pipeClient = new PipeClientWithCallback<IAdder, IConcatenator>("mypipe", () => new Concatenator());
await pipeClient.ConnectAsync();
int result = await pipeClient.InvokeAsync(a => a.AddNumbers(4, 7));
```

### About the library
This library uses named pipes to invoke method calls on a remote endpoint. The method arguments are packaged up in JSON, encoded and sent over the pipe.

### Features
* 100% asynchronous communication with .ConfigureAwait(false) to minimize context switches and reduce thread use
* 39KB with Newtonsoft JSON as only dependency
* Invoking async methods
* Passing and returning complex types via JSON serialization
* Interleaved or multiple simultaneous calls
* Throwing exceptions
* CancellationToken support

### Not supported
* Methods with out and ref parameters
* Properties
* Method overloads
