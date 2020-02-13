using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalR.ClientProxy
{
	/// <summary>
	/// <b>Do not use the Concrete type as T at this time.</b>
	/// </summary>
	/// <typeparam name="T"></typeparam>
    public class HubReceiverContainer<T> : IDisposable
{
	private Dictionary<MethodInfo, IDisposable> _toDispose = new Dictionary<System.Reflection.MethodInfo, System.IDisposable>();
	public T Handler { get; private set; }
	public HubReceiverContainer(HubConnection conn, T instance)
	{
		if (typeof(T).IsClass || typeof(T).IsValueType)
		{
			throw new ArgumentException($"{nameof(instance)} must be an Interface!", nameof(instance));
		}
		Handler = instance;
		_toDispose = HubReceiverBuilder.CreateClientReceiver(conn, instance);

	}

	public void Dispose()
	{
		try
		{
			if (_toDispose?.Any() ?? false)
			{
				foreach (var toDispose in _toDispose)
				{
					try
					{
						toDispose.Value.Dispose();
					}
					catch
					{
					}

				}
			}

		}
		catch
		{
			//intentional;disposing
		}
	}
}
}