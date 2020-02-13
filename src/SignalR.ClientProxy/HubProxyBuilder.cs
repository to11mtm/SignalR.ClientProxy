using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalR.ClientProxy
{
    public class HubProxyBuilder
    {
        private static ConcurrentDictionary<Type,Type> _builtTypeCache = new ConcurrentDictionary<System.Type, System.Type>();
        /// <summary>
        /// Creates a Proxy for calling a hub that implements an interface T.
        /// </summary>
        /// <param name="conn"></param>
        /// <typeparam name="T">The interface being implemented.</typeparam>
        /// <returns></returns>
        public static T CreateProxy<T>(HubConnection conn)
        {
            var type = _builtTypeCache.GetOrAdd(typeof(T), (t)=> _buildMethod<T>());
            return (T)Activator.CreateInstance(type,conn);
        }
        private static Type _buildMethod<T>()
        {
            var type = typeof(T);
            var asmNameString = "GlutenFree.SignalR.Client.ServerProxy.Generated.ForType."+type.Name;
            var modName ="proxy_"+type.Name+"_module";
            var typeName ="proxy_"+type.Name;
            MethodInfo noCancelTokenMi = typeof(CancellationToken)
                .GetProperty("None", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod();

            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(asmNameString), AssemblyBuilderAccess.RunAndCollect);
            var module = ab.DefineDynamicModule(modName);
            var tb = module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { typeof(T) });
            var fb = tb.DefineField("_hub", typeof(HubConnection), FieldAttributes.Public);
		
            var ctor = Sigil.NonGeneric.Emit.BuildConstructor(new[] { typeof(Microsoft.AspNetCore.SignalR.Client.HubConnection) }, tb, MethodAttributes.Public);
            ctor.LoadArgument(0);
            ctor.LoadArgument(1);
            ctor.StoreField(fb);
            ctor.Return();
            var cb = ctor.CreateConstructor();

            var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var m in methods)
            {
                var parameters = m.GetParameters();
                var im = Sigil.NonGeneric.Emit.BuildInstanceMethod(m.ReturnType,
                    parameters.Select(par => par.ParameterType).ToArray(), 
                    tb, 
                    m.Name, 
                    MethodAttributes.Public | MethodAttributes.Virtual);

                //object[] objArr new object[parameters.Length]
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
                    //Empty the stack to store into array.
                    im.StoreElement(typeof(object));
                }
			
                //return HubExtender.PerformSendCoreAsync(_hub,name,objArr,CancellationToken.None);
                im.LoadArgument(0);
                im.LoadField(fb);
                im.LoadConstant(m.Name);
                im.LoadLocal(objArr);
                im.Call(noCancelTokenMi);
                var hubMethod = typeof(HubExtender).GetMethod(nameof(HubExtender.PerformSendCoreAsync));
                im.Call(hubMethod);
                im.Return();
                im.CreateMethod();
			
            }
            var builtType = tb.CreateTypeInfo();
            module.CreateGlobalFunctions();
            return builtType;
        }
    }
}