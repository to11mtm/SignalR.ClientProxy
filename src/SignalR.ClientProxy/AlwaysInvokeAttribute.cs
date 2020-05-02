using System;

namespace GlutenFree.SignalR.ClientProxy
{
    /// <summary>
    /// Specifies that a hub method should always be treated as `Invoke` and not `Send`
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AlwaysInvokeAttribute : Attribute
    {
        
    }
}