using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace GlutenFree.SignalR.ClientReceiver
{
    public class HubReceiverBuilder
    {
        private static ConcurrentDictionary<Type, Type> _builtTypeCache =
            new ConcurrentDictionary<System.Type, System.Type>();

        /// <summary>
        /// Wires up a Client Receiver to a hub.
        /// If you want to manage disposal yourself, use this instead of
        /// <see cref="HubReceiverContainer{T}"/>
        /// <b> You Specify use the interface type and not a concrete class as T </b>
        /// </summary>
        /// <param name="instance">The Hub Connection</param>
        /// <param name="instance"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A Dictionary of Disposables for each MethodInfo hooked.</returns>
        public static Dictionary<MethodInfo, IDisposable>
            CreateClientReceiver<T>(HubConnection conn, T instance)
        {
            if (typeof(T).IsClass || typeof(T).IsValueType)
            {
                throw new ArgumentException($"{nameof(instance)} must be an Interface!", nameof(instance));
            }
            Dictionary<MethodInfo, IDisposable> results =
                new Dictionary<System.Reflection.MethodInfo, System.IDisposable
                >();


            var m = typeof(T).GetMethods();

            //wire up each method.
            foreach (var me in m)
            {
                //Do not Autowire non-task methods
                //Technically they aren't allowed by the server anyway...
                if (me.ReturnType != typeof(Task))
                    continue;
                //TODO: the client side currently uses a Compiled expression instead of an Emitted method.
                //This is probably a little more readable,
                //But it is not going to be quite as performant.
                //(It'll probably be fine :))
                var mainArg = Expression.Parameter(typeof(T));
                var objArrArg = Expression.Parameter(typeof(object[]));
                List<Expression> accessExprs = new List<Expression>();

                var pars = me.GetParameters().Select(r=>r.ParameterType).ToArray();
                for (int i = 0; i < pars.Length; i++)
                {
                    accessExprs.Add(Expression.Convert(
                        Expression.ArrayIndex(objArrArg,
                            Expression.Constant(i)), pars[i]));
                }

                var mc = Expression.Call(mainArg, me, accessExprs);
                
                //curriedInvoker
                //hubConection.On("TMethod","MethodName", ParameterTypes, curriedInvokerInvoke)
                var res = Expression.Lambda<Func<T,object[],Task>>(mc,mainArg, objArrArg);
                var func = res.Compile();
                results.Add(me, conn.On(me.Name,
                    pars,
                    async (o) => await func(instance,o)));
            }

            return results;
        }
    }
}