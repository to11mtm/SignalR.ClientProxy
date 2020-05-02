using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace GlutenFree.SignalR.ClientProxy
{
    internal enum returnKindEnum
    {
        voidType,
        TaskType,
        ReturnType,
        TaskReturnType,
        AsyncEnumReturnType,
        TaskAsyncEnumReturnType,
        TaskChannelReaderReturnType,
        ChannelReaderReturnType
    }

    public class HubProxyBuilder
    {
        private static ConcurrentDictionary<Type,Type> _invokeTypeCache = new ConcurrentDictionary<Type, Type>();
        private static ConcurrentDictionary<Type,Type> _builtTypeCache = new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// Creates a Proxy for calling a hub that implements an interface T.
        /// </summary>
        /// <param name="conn">The <see cref="HubConnection"/> that the proxy will use</param>
        /// <param name="useInvokeForAll">if -true- <see cref="Void"/> and <see cref="Task"/> returning calls will be treated as hub invocations rather than sends.</param>
        /// <typeparam name="TProxy">The interface being implemented.</typeparam>
        /// <returns>A <see cref="TProxy"/> that will send the message on calls via <see cref="HubConnection"/>.</returns>
        public static TProxy CreateProxy<TProxy>(HubConnection conn, bool useInvokeForAll = false)
        {
            if (typeof(TProxy).IsClass || typeof(TProxy).IsValueType)
            {
                throw new ArgumentException("Type of TProxy Must be an interface!");
            }

            var cts = new CancellationTokenSource();
            
            var type =
                (useInvokeForAll ? _invokeTypeCache : _builtTypeCache).GetOrAdd(
                    typeof(TProxy),
                    (t) => _buildMethod<TProxy>(useInvokeForAll));
            return (TProxy)Activator.CreateInstance(type,conn);
        }


        public static bool IsTaskReturnType(Type theType)
        {
            return theType.IsConstructedGenericType &&
                   theType.GetGenericTypeDefinition() == typeof(Task<>);
        }

        public static bool IsAsyncEnumerableType(Type theType)
        {
            return theType.IsConstructedGenericType &&
                   theType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>);
        }

        public static bool IsChannelReaderType(Type theType)
        {
            return theType.IsConstructedGenericType &&
                   theType.GetGenericTypeDefinition() ==
                   typeof(ChannelReader<>);
        }

        public static Type GetInnerReturnType(Type theType)
        {
            return theType.GetGenericArguments()[0];
        }
        private static Type _buildMethod<T>(bool useInvokeForAll = false)
        {
            var type = typeof(T);
            var asmNameString = "GlutenFree.SignalR.Client.ServerProxy.Generated.ForType."+type.Name;
            var modName =
                $"proxy_{type.Name}_{(useInvokeForAll ? "invoke" : "send")}_module";
            var typeName = $"proxy_{type.Name}_{(useInvokeForAll ? "invoke" : "send")}";
            MethodInfo contextCancelTokenMi = typeof(HubProxy).GetMethod(nameof(HubProxy.ContextCancellationToken));
            /* typeof(CancellationToken)
                .GetProperty(nameof(CancellationToken.None),
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Static).GetGetMethod();*/
            var getAwaiterMC = typeof(Task).GetMethod("GetAwaiter");
            var getAwaiterResultMC = typeof(TaskAwaiter).GetMethod("GetResult");

            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(asmNameString), AssemblyBuilderAccess.RunAndCollect);
            
            var module = ab.DefineDynamicModule(modName);
            
            //public class proxy_IProxy_invokeOrSend : IProxy
            //{
            //   private HubConnection _hub;
            var tb = module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { typeof(T) });
            var fb = tb.DefineField("_hub", typeof(HubConnection), FieldAttributes.Public);
		
            // public proxy_IProxy_invokeOrSend(hubConnection conn)
            // {
            var ctor = global::Sigil.NonGeneric.Emit.BuildConstructor(new[] { typeof(Microsoft.AspNetCore.SignalR.Client.HubConnection) }, tb, MethodAttributes.Public);
            //this._hub = conn;
            //Instance&NonGeneric method means LoadArg(0) puts 'this' on stack
            ctor.LoadArgument(0);
            ctor.LoadArgument(1);
            ctor.StoreField(fb);
            //Stack empty.
            // }
            ctor.Return();
            var cb = ctor.CreateConstructor();

            var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var m in methods)
            {
                var parameters = m.GetParameters();

                // public TMethodReturn IProxy.IMethod(Targ1 arg1,TArg2 arg2...)
                // {
                var im = global::Sigil.NonGeneric.Emit.BuildInstanceMethod(m.ReturnType,
                    parameters.Select(par => par.ParameterType).ToArray(),
                    tb,
                    m.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual);

                // object[] objArr new object[parameters.Length];
                var objArr = im.DeclareLocal(typeof(object[]), "objArr");
                im.LoadConstant(parameters.Length);
                im.NewArray(typeof(object));
                im.StoreLocal(objArr);

                
                //for (int i=0; i < parameters.Length; i++)
                //{
                //   objArr[i] = (object)parameters[i];	
                //}
                //Well, not quite. technically this is an unrolled loop:
                // objarr[0] = (object)parameters[0];
                // objarr[1] = (object)parameters[1];
                //And we only cast if needed.
                //Put Each parameter into the payload object.
                for (int i = 0; i < parameters.Length; i++)
                {
                    //Push Array and index onto stack.
                    im.LoadLocal(objArr);
                    im.LoadConstant(i);
                    //Fun facts about uShort;
                    //-It's not part of the CLS
                    //-Yet MSIL says you use a UShort for Loadarg
                    //-Any adding of a UShort expands the type to int
                    //So you just always cast here.

                    //Push Arg onto stack.
                    im.LoadArgument((ushort) (i + 1));
                    //If need boxing, box. Stack is same.
                    if (false == (parameters[i].ParameterType.IsClass ||
                                  parameters[i].ParameterType.IsInterface))
                    {
                        im.Box(parameters[i].ParameterType);
                    }

                    //Stack: objArr-i-parameter[i] (as object)
                    //Empty the stack to store into array.
                    im.StoreElement(typeof(object));
                }

                
                // return HubExtender
                //  .PerformSendCoreAsync(_hub,name,objArr,HubProxy.ContextCancellationToken());
                //or
                //  .PerformInvokeCoreAsync<TReturn>(_hub,name,objArr,HubProxy.ContextCancellationToken());
                //or
                //  .PerformStreamAsync<IAsyncEnumerable<TReturn>>(_hub,name,objArr,HubProxy.ContextCancellationToken());
                im.LoadArgument(0);
                im.LoadField(fb);
                im.LoadConstant(m.Name);
                im.LoadLocal(objArr);
                im.Call(contextCancelTokenMi);
                var sendTypes = new[]
                {
                    typeof(Task),
                    typeof(void)
                };
                var returnType = m.ReturnType;
                var rt = GetReturnType(m);
                //If a Task<TResult>, we need to get the type of the TResult. Kinda ugly here,
                //But since this is called once per method for the life of the appdomain we can live with it.
                if (rt == returnKindEnum.TaskReturnType ||
                    rt == returnKindEnum.AsyncEnumReturnType || rt == returnKindEnum.ChannelReaderReturnType)
                {
                    returnType = m.ReturnType.GetGenericArguments()[0];
                }

                if (rt == returnKindEnum.TaskAsyncEnumReturnType || rt == returnKindEnum.TaskChannelReaderReturnType)
                {
                    returnType = m.ReturnType.GenericTypeArguments[0].GenericTypeArguments[0];
                }
                
                
                //If we are dealing with a Return of Task or void,
                //Only use Invoke if they want it.
                var hubMethod =
                    CreateHubMethod(useInvokeForAll,m ,returnType, rt);
                im.Call(hubMethod);
                //Stack: Task from PerformSendCoreAsync/PerformInvokeCoreAsync(passed back from Send/InvokeCoreAsync)
                //or IASyncEnumerable<TReturn> from PerformStreamAsync<TReturn>;
                //if Void, we cannot return the task, that's bad IL
                //If so, Run it synchronously.
                if (rt == returnKindEnum.voidType)
                {
                    //.GetAwaiter().GetResult();
                    var awaiterSlot = im.DeclareLocal(typeof(TaskAwaiter));
                    im.Call(getAwaiterMC);
                    im.StoreLocal(awaiterSlot);
                    im.LoadLocalAddress(awaiterSlot);
                    im.Call(getAwaiterResultMC);
                }
                //If Not a Task<TResult> (or a normal Task,)
                //We need to get the awaiter and result accordingly.
                else if (rt == returnKindEnum.ReturnType || rt == returnKindEnum.ChannelReaderReturnType)
                {
                    //.GetAwaiter().GetResult();
                    var ttype =
                        typeof(Task<>).MakeGenericType(new[] {m.ReturnType});
                    var getAwaitRC = ttype.GetMethod("GetAwaiter");
                    var awaiterReturn =
                        typeof(TaskAwaiter<>).MakeGenericType(m.ReturnType);
                    var madeCall = awaiterReturn.GetMethod("GetResult");
                    var awaiterSlot = im.DeclareLocal(awaiterReturn);
                    im.Call(getAwaitRC);
                    im.StoreLocal(awaiterSlot);
                    im.LoadLocalAddress(awaiterSlot);
                    im.Call(madeCall);

                }

                string trash = "";
                im.Return();
                var d = im.CreateMethod(out trash);
            }
            var builtType = tb.CreateTypeInfo();
            module.CreateGlobalFunctions();
            return builtType;
        }

        /// <summary>
        /// Gets what 'kind' of return the method provides. 
        /// </summary>
        private static returnKindEnum GetReturnType(MethodInfo m)
        {
            returnKindEnum rt = 0;
            if (m.ReturnType == typeof(void))
            {
                rt = returnKindEnum.voidType;
            }
            else if (m.ReturnType == typeof(Task) &&
                     m.ReturnType.IsConstructedGenericType == false)
            {
                rt = returnKindEnum.TaskType;
            }
            else if (IsTaskReturnType(m.ReturnType))
            {
                if (IsAsyncEnumerableType(GetInnerReturnType(m.ReturnType)))
                {
                    rt = returnKindEnum.TaskAsyncEnumReturnType;
                }
                else if (IsChannelReaderType(GetInnerReturnType(m.ReturnType)))
                {
                    rt = returnKindEnum.TaskChannelReaderReturnType;
                }
                else
                {
                    rt = returnKindEnum.TaskReturnType;    
                }
                
            }
            else if (IsAsyncEnumerableType(m.ReturnType))
            {
                rt = returnKindEnum.AsyncEnumReturnType;
            }
            else if (IsChannelReaderType(m.ReturnType))
            {
                rt = returnKindEnum.ChannelReaderReturnType;
            }
            else
            {
                rt = returnKindEnum.ReturnType;
            }

            return rt;
        }

        /// <summary>
        /// Determines which method to call for the invocation
        /// </summary>
        /// <returns>The selected Method on the <see cref="HubConnection"/> to use.</returns>
        private static MethodInfo CreateHubMethod(bool useInvokeForAll,MethodInfo method, Type retType, returnKindEnum rett)
        {
            switch (rett)
            {
                case returnKindEnum.voidType:
                case returnKindEnum.TaskType:
                    //Firing order:
                    //      AlwaysInvoke? ->Invoke
                    //      AlwaySend?  ->Send
                    //      invokeForAll true->Invoke
                    //                   false->Send
                    return ((useInvokeForAll &&
                             method.GetCustomAttributes()
                                 .Any(a => a is AlwaysSendAttribute) ==
                             false) || method.GetCustomAttributes()
                        .Any(a => a is AlwaysInvokeAttribute))
                        ? typeof(HubExtender).GetMethod(
                            nameof(HubExtender.PerformInvokeCoreAsyncNoReturn))
                        : typeof(HubExtender).GetMethod(
                            nameof(HubExtender.PerformSendCoreAsync));
                case returnKindEnum.AsyncEnumReturnType:
                    //If getting back an async enum, we need to stream.
                    return typeof(HubExtender)
                        .GetMethod(
                            nameof(HubExtender.PerformStreamCoreAsync))
                        ?.MakeGenericMethod(new[] {retType});
                case returnKindEnum.TaskAsyncEnumReturnType:
                    //Edge case: The API for once isn't returning a Task
                    //So PerformStreamCoreAsyncTask wraps in a task for us.
                    return typeof(HubExtender)
                        .GetMethod(
                            nameof(HubExtender.PerformStreamCoreAsyncTask))
                        ?.MakeGenericMethod(new[] {retType});
                case returnKindEnum.TaskChannelReaderReturnType:
                case returnKindEnum.ChannelReaderReturnType:
                    //ChannelReader is given back as Task<ChannelReader<T>>
                    //So we follow GetAwaiter().GetResult() semantics here.
                    return typeof(HubExtender)
                        .GetMethod(
                            nameof(HubExtender.PerformChannelReadStreamCoreAsync))
                        ?.MakeGenericMethod(new[] {retType});
                case returnKindEnum.TaskReturnType:
                case returnKindEnum.ReturnType:
                    default:
                    //Logically speaking this is default regardless.
                    return typeof(HubExtender)
                        .GetMethod(
                            nameof(HubExtender.PerformInvokeCoreAsyncReturn))
                        ?.MakeGenericMethod(new[] {retType});
                
                
                    
            }
        }
    }
}