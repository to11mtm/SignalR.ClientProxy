using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalR.ClientProxy
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
        /// <param name="conn">The Hub Connection</param>
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

            //Can we stop for a second and talk about how this overload is hidden?
            //It's the most useful one for building a proxy, but it's private.
            var method = typeof(HubConnectionExtensions)
                .GetMethod(name: "On",
                    bindingAttr: BindingFlags.NonPublic | BindingFlags.Static,
                    binder: null,
                    types: new[]
                    {
                        typeof(HubConnection), typeof(string), typeof(Type[]),
                        typeof(Action<object[]>)
                    },
                    modifiers: new ParameterModifier[] { });

            var m = typeof(T).GetMethods();

            //wire up each method.
            foreach (var me in m)
            {
                //TODO: the client side currently uses a Compiled expression instead of an Emitted method.
                //This is probably a little more readable,
                //But it is not going to be quite as performant.
                //(It'll probably be fine :))
                var mainArg = Expression.Parameter(typeof(T));
                var objArrArg = Expression.Parameter(typeof(object[]));
                List<Expression> accessExprs = new List<Expression>();

                var pars = me.GetParameters();
                for (int i = 0; i < pars.Length; i++)
                {
                    accessExprs.Add(Expression.Convert(
                        Expression.ArrayIndex(objArrArg,
                            Expression.Constant(i)), pars[i].ParameterType));
                }

                var mc = Expression.Call(mainArg, me, accessExprs);
                var res =
                    Expression.Lambda<Action<T, object[]>>(mc, mainArg,
                        objArrArg);
                var func = res.Compile();
                Action<object[]> curried = (o) => func(instance, o);
                results.Add(me,
                    (IDisposable) method.Invoke(null,
                        new object[]
                        {
                            conn, me.Name,
                            me.GetParameters().Select(x => x.ParameterType)
                                .ToArray(),
                            curried
                        }));

            }

            return results;
        }
    }
}