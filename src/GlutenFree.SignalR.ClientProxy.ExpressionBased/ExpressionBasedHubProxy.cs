using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace GlutenFree.SignalR.ClientProxy.ExpressionBased
{
    /// <summary>
    /// A Proxy for use in scenarios where IL Generation is not desired
    /// Or Permitted.
    /// <para>
    /// Using this proxy generates much more garbage than the 
    /// <see cref="HubProxyBuilder"/> generated types, as rather than Clean IL,
    /// We are having to Look at a new expression tree every call to determine
    /// Arguments sent, etc.
    /// </para>
    /// </summary>
    /// <typeparam name="TProxy">The type of the Proxy to use</typeparam>
    public class ExpressionBasedHubProxy<TProxy>
    {
        private HubConnection _conn;

        public ExpressionBasedHubProxy(HubConnection conn)
        {
            _conn = conn;
        }

        public async Task Execute(
            Expression<Func<TProxy, Task>> serverCall)
        {
            var mcEx = ValidateCall(serverCall);
                var args = ExpressionBasedParser.CreateInternal(mcEx);
                await _conn.SendCoreAsync(mcEx.Method.Name,
                    args.Select(r => r.Value).ToArray()).ConfigureAwait(false);

        }

        public void Execute(
            Expression<Action<TProxy>> serverCall)
        {
            var mcEx = ValidateCall(serverCall);
            var args = ExpressionBasedParser.CreateInternal(mcEx);
            _conn.SendCoreAsync(mcEx.Method.Name,
                    args.Select(r => r.Value).ToArray()).ConfigureAwait(false)
                .GetAwaiter().GetResult();

        }

        public async Task<TResult> Invoke<TResult>(
            Expression<Func<TProxy, Task<TResult>>> serverCall)
        {
            
            var mcEx = ValidateCall(serverCall);
                var m = mcEx.Method;
                var retType =typeof(TResult);
                var args = ExpressionBasedParser.CreateInternal(mcEx);
                return (TResult)_conn.InvokeCoreAsync(mcEx.Method.Name, retType, 
                    args.Select(r => r.Value).ToArray()).ConfigureAwait(false).GetAwaiter().GetResult();
            
        }
        
        public async Task<TResult> InvokeAsync<TResult>(
            Expression<Func<TProxy, Task<TResult>>> serverCall)
        {
            
            var mcEx = ValidateCall(serverCall);
            
                var m = mcEx.Method;
                var retType = typeof(TResult);
                var args = ExpressionBasedParser.CreateInternal(mcEx);
                return (TResult)await _conn.InvokeCoreAsync(mcEx.Method.Name, retType, 
                    args.Select(r => r.Value).ToArray()).ConfigureAwait(false);
            
        }

        public async Task<TResult> InvokeAsync<TResult>(
            Expression<Func<TProxy, TResult>> serverCall)
        {
            var mcEx = ValidateCall(serverCall);
            var m = mcEx.Method;
            var retType = m.ReturnType;
            var args = ExpressionBasedParser.CreateInternal(mcEx);
            return (TResult) await _conn.InvokeCoreAsync(mcEx.Method.Name,
                retType,
                args.Select(r => r.Value).ToArray()).ConfigureAwait(false);

        }

        private static MethodCallExpression ValidateCall(LambdaExpression expression)
        {
            if (expression.Body is MethodCallExpression mcEx &&
                mcEx.Method.DeclaringType == typeof(TProxy))
            {
                return mcEx;
            }
            else
            {
                throw  new ArgumentException("serverCall must be a call to " + typeof(TProxy).Name);
            }
        }
        
        public TResult Invoke<TResult>(
            Expression<Func<TProxy, TResult>> serverCall)
        {
            
            if (serverCall.Body is MethodCallExpression mcEx)
            {
                var m = mcEx.Method;
                var retType = m.ReturnType;
                var args = ExpressionBasedParser.CreateInternal(mcEx);
                return (TResult) _conn.InvokeCoreAsync(mcEx.Method.Name, retType, 
                    args.Select(r => r.Value).ToArray()).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                throw  new ArgumentException(serverCall.ToString());
            }
        }
    }
    /// <summary>
    /// A Helper to get the correct data to Invoke a Server Proxy call.
    /// </summary>
    public static class ExpressionBasedParser
    {
        private static readonly ConcurrentDictionary<MethodInfo, string[]>
            paramNameDictionary =
                new ConcurrentDictionary<MethodInfo, string[]>();

        private static readonly Type _objectType = typeof(object);

        public static CallParameter[] CreateInternal(
            MethodCallExpression methodCall,
            ParameterExpression parameterExpression = null)
        {
         

            var argProv = methodCall.Arguments;

            var argCount = argProv.Count;

            string[] paramNames;
            var methodInfo = methodCall.Method;

            paramNames = paramNameDictionary.GetOrAdd(methodInfo,
                (mi) => mi.GetParameters().Select(r => r.Name).ToArray());


            return ParseCallArgs(argCount, argProv, paramNames);


        }

        /// <summary>
        /// Parses the arguments for the method call contained in the expression.
        /// </summary>
        private static CallParameter[] ParseCallArgs(int argCount,
            ReadOnlyCollection<Expression> argProv,string[] paramNames)
        {
            CallParameter[] _jobArgs = new CallParameter[argCount];
            for (int i = 0; i < argCount; i++)
            {
                var theArg = argProv[i];
                object val = null;
                try
                {
                    if (theArg is ConstantExpression _theConst)
                    {
                        //Happy Case.
                        //If constant, no need for invokes,
                        //or anything else
                        val = _theConst.Value;
                    }
                    else
                    {
                        bool memSet = false;
                        if (theArg is MemberExpression)
                        {
                            if (theArg is MemberExpression _memArg)
                            {
                                if (_memArg.Expression is ConstantExpression c)
                                {
                                    if (_memArg.Member is FieldInfo f)
                                    {
                                        val = f.GetValue(c.Value);
                                        memSet = true;
                                    }
                                    else if (_memArg.Member is PropertyInfo p)
                                    {
                                        val = p.GetValue(c.Value);
                                        memSet = true;
                                    }
                                }
                            }
                        }

                        if (memSet == false)
                        {
                            //If we are dealing with a Valuetype, we need a convert.
                            var convArg = ConvertIfNeeded(theArg);

                            val = CompileExprWithConvert(Expression
                                    .Lambda<Func<object>>(
                                        convArg))
                                .Invoke();


                        }
                    }

                    _jobArgs[i] = new CallParameter()
                    {
                        Name = paramNames[i],
                        Value = val
                    };
                }
                catch (Exception exception)
                {
                    //Fallback. Do the worst way.
                    try
                    {
                        object fallbackVal;
                        {
                            fallbackVal = Expression.Lambda(
                                    Expression.Convert(theArg, _objectType)
                                )
                                .Compile().DynamicInvoke();
                        }
                        _jobArgs[i] = new CallParameter()
                        {
                            Name = paramNames[i],
                            Value = fallbackVal
                        };
                    }

                    catch (Exception ex)
                    {
                        throw new ParseArgumentException(
                            "Couldn't derive value from Expression! Please use variables whenever possible",
                            ex);
                    }
                }
            }

            return _jobArgs;
        }
        

        public static Expression ConvertIfNeeded(Expression toConv)
        {
            Type retType = null;
            if (toConv.NodeType == ExpressionType.Lambda)
            {
                retType = TraverseForType(toConv.Type.GetGenericArguments()
                    .LastOrDefault());
            }
            else
            {
                retType = toConv.Type;
            }

            if (retType?.BaseType == _objectType)
            {
                return toConv;
            }
            else
            {
                return Expression.Convert(toConv, _objectType);
            }
        }

        public static Type TraverseForType(Type toConv)
        {
            if (toConv == null)
            {
                return null;
            }
            else if (toConv == typeof(MulticastDelegate))
            {
                //I don't think this should happen in sane usage, but let's cover it.
                return (TraverseForType(toConv.GetGenericArguments().LastOrDefault()));
            }
            else
            {
                return toConv.GetType();
            }
        }
        private static T CompileExprWithConvert<T>(Expression<T> lambda) where T : class
        {
                return lambda.Compile();
        }
    }

    public class CallParameter
    {
        public object Value { get; set; }
        public string Name { get; set; }
    }

    public class ParseArgumentException : Exception
    {
        public ParseArgumentException(string message, Exception ex):base(message,ex)
        {
            
        }
    }
}