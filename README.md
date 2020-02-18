# SignalR.ClientProxy
A Set of Proxy Generators for SignalR Core

All Examples will assume the following:

```
public interface IServer
{
  Task Join(string name, DateTime expiresAt);
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

Please note that (primarily due to the fact SignalR enforces similar/same rules) that Client side interface Methods *must* return a type of Task to be used by this library, and Server interface methods should return `Task` or `void` (`Task` is still preferred, the use of `void` is not fully tested and may at this time produce undesired behavior/deadlocks.) 

# HubProxyBuilder

`HubProxyBuilder` creates a Typed Server client.
The proxy may be treated as an object with the same lifetime as `HubConnection`.
```
var serverProxy = HubProxyBuilder.CreateProxy<IServer>(hubConnection);
await serverProxy.Join("GF-Solutions", DateTime.UtcNow.AddSeconds(300));

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
