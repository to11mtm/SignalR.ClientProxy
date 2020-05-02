using System;

namespace GlutenFree.SignalR.ClientProxy
{
    /// <summary>
    /// Specifies that a hub method should always be treated as 'Send'
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AlwaysSendAttribute : Attribute
    {
        
    }
}