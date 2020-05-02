# SignalR.ClientProxy
A Set of Proxy Generators for SignalR Core

## Quick Start:

Download the `GlutenFree.SignalR.ClientProxy` package from Nuget and get on with your life. :)

All Examples will assume the following:

```
public interface IServer
{
  Task Join(string name, DateTime expiresAt);
  Task<DateTime> GetExpiration(string name);
}
public interface IClient
{
  Task Message(string msg);
}
public class MyHub<IClient> : IServer
{
  //impls
}
```


# HubProxyBuilder / HubProxy

`HubProxyBuilder` creates a Typed Server client.
The proxy may be treated as an object with the same lifetime as `HubConnection`.
```
var serverProxy = HubProxyBuilder.CreateProxy<IServer>(hubConnection, useInvokeForAll: false);
await serverProxy.Join("GF-Solutions", DateTime.UtcNow.AddSeconds(300));

```

You may also use `HubProxy.Create` as a convenience method.

Please note that (primarily due to the fact SignalR enforces similar/same rules) that Client side interface Methods *must* return a type of Task to be used by this library. 

Server Interface methods are defined by the following rules:

  - Methods Returning `Task` or `void` will be sent with `SendCoreAsync` unless `useInvokeForAll` is passed into the HubProxyBuilder as `true`. This means that they will only throw an error if the server didn't -get- the response.
  
  - Methods Returning `Task<TResult>` or Some other type will be sent with `InvokeCoreAsync`. This means that they will get some level of error if the server failed processing.
  
  - Methods Returning `IAsyncEnumerable<TResult>` or `Task<IAsyncEnumerable<TResult>` will be sent with `StreamAsyncCore`. This means they will return an enumerator.
    - In the case of `Task<IAsyncEnumerable>>`, ASPNETCORE does not return in a Task so we wrap.
  
  - Methods Returning `ChannelReader<TResult>` or `Task<ChannelReader<TResult>>` will be sent with `StreamAsChannelCoreAsync`.
    - In the case of `ChannelReader<TResult>`, we 
  
  - Streaming methods That pass in an `IAsyncEnumerable<TInput>` will be called with the same convention as other methods.
    - This means that unless you pass `true` into the `HubProxyBuilder` the call will be non-blocking. Make sure you know what you're getting into!

You may use the `AlwaysInvokeAttribute` and `AlwaysSendAttribute` on your `Task` or `void` returning methods if you require more granular behavior. These attributes on your interface methods will cause the type to always be treated as an Invoke or Send, respectively.

## Cancellation

If you wish to use CancellationTokens, There is a series of `HubProxy.WithCancellation` static methods that let you provide a cancellation token to be used for the request:
```
var client = HubProxyBuilder.CreateProxy<IBar>(hc);
var cts = new CancellationTokenSource();
var res = HubProxy.WithCancellationToken(client, cts.Token,
r => r.StreamFromServer("merp"));
//var res = client.StreamFromServer("nerp");
List<int> results = new List<int>();
            
await Assert.ThrowsAsync<TaskCanceledException>(async () =>
{
    var counter = 0;
    await foreach (var myInt in res)
    {
        results.Add(myInt);
        counter = counter + 1;
        if (counter==2)
        cts.Cancel();
    }
});

Assert.Equal(2,results.Count);
```

# HubReceiverContainer

`HubReceiverContainer` is a container for autowiring Client Receivers.
Matching interface methods that return `Task` will be hooked up as listeners to the connection.
The returned Container is Disposable, and should be disposed when you wish for
The handlers to stop firing for a given connection.
```
public class ClientRec : IClient
{
  public Task Message(string msg)
  {
    System.Console.WriteLine(msg);
  }
}

// use:
//Methods inside clientRecInstance
//will be called when the server sends them.
IDisposable wiredClient = new HubReceiverContainer<IClient>(hubConnection, clientRecInstance);
```
