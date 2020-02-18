using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace GlutenFree.SignalR.ClientProxy
{
    public class HubProxyBuilder
    {
        private static ConcurrentDictionary<Type,Type> _builtTypeCache = new ConcurrentDictionary<System.Type, System.Type>();
        /// <summary>
        /// Creates a Proxy for calling a hub that implements an interface T.
        /// </summary>
        /// <param name="conn">The <see cref="HubConnection"/> that the proxy will use</param>
        /// <typeparam name="TProxy">The interface being implemented.</typeparam>
        /// <returns>A <see cref="TProxy"/> that will send the message on calls via <see cref="HubConnection"/>.</returns>
        public static TProxy CreateProxy<TProxy>(HubConnection conn)
        {
            if (typeof(TProxy).IsClass || typeof(TProxy).IsValueType)
            {
                throw new ArgumentException("Type of TProxy Must be an interface!");
            }
            var type = _builtTypeCache.GetOrAdd(typeof(TProxy), (t)=> _buildMethod<TProxy>());
            return (TProxy)Activator.CreateInstance(type,conn);
        }
        private static Type _buildMethod<T>()
        {
            var type = typeof(T);
            var asmNameString = "GlutenFree.SignalR.Client.ServerProxy.Generated.ForType."+type.Name;
            var modName ="proxy_"+type.Name+"_module";
            var typeName ="proxy_"+type.Name;
            MethodInfo noCancelTokenMi = typeof(CancellationToken)
                .GetProperty(nameof(CancellationToken.None),
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Static).GetGetMethod();
            var getAwaiterMC = typeof(Task).GetMethod("GetAwaiter");
            var getAwaiterResultMC = typeof(TaskAwaiter).GetMethod("GetResult");

            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(asmNameString), AssemblyBuilderAccess.RunAndCollect);
            var module = ab.DefineDynamicModule(modName);
            
            //public class proxy_IProxy : IProxy
            //{
            //   private HubConnection _hub;
            var tb = module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { typeof(T) });
            var fb = tb.DefineField("_hub", typeof(HubConnection), FieldAttributes.Public);
		
            // public proxy_IProxy(hubConnection conn)
            // {
            var ctor = Sigil.NonGeneric.Emit.BuildConstructor(new[] { typeof(Microsoft.AspNetCore.SignalR.Client.HubConnection) }, tb, MethodAttributes.Public);
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
                var im = Sigil.NonGeneric.Emit.BuildInstanceMethod(m.ReturnType,
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
                    im.LoadArgument((ushort)(i + 1));
                    //If need boxing, box. Stack is same.
                    if (false == (parameters[i].ParameterType.IsClass || parameters[i].ParameterType.IsInterface))
                    {
                        im.Box(parameters[i].ParameterType);
                    }
                    //Stack: objArr-i-parameter[i] (as object)
                    //Empty the stack to store into array.
                    im.StoreElement(typeof(object));
                }
			
                // return HubExtender.PerformSendCoreAsync(_hub,name,objArr,CancellationToken.None);
                im.LoadArgument(0);
                im.LoadField(fb);
                im.LoadConstant(m.Name);
                im.LoadLocal(objArr);
                im.Call(noCancelTokenMi);
                var hubMethod = typeof(HubExtender).GetMethod(nameof(HubExtender.PerformSendCoreAsync));
                im.Call(hubMethod);
                //Stack: Task from PerformSendCoreAsync(passed back from SendCoreAsync)
                
                //if Void, we cannot return the task, that's bad IL
                //If so, Run it synchronously.
                if (m.ReturnType == typeof(void))
                {
                    //.GetAwaiter().GetResult();
                    var awaiterSlot = im.DeclareLocal(typeof(TaskAwaiter));
                    im.Call(getAwaiterMC);
                    im.StoreLocal(awaiterSlot);
                    im.LoadLocalAddress(awaiterSlot);
                    im.Call(getAwaiterResultMC);
                }                
                // }
                im.Return();
                im.CreateMethod();
			
            }
            var builtType = tb.CreateTypeInfo();
            module.CreateGlobalFunctions();
            return builtType;
        }
    }
}