using System;
using System.Linq;
using System.Reflection;
using ICSharpCode.CodeConverter.Util;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// Inspired by: https://codeblog.jonskeet.uk/2008/08/09/making-reflection-fly-and-exploring-delegates/
    /// </summary>
    internal static class MethodInfoExtensions
    {
        public static TFunc CreateOpenDelegate<TFunc>(this MethodInfo method) where TFunc : Delegate
        {
            return (TFunc)method.CreateDelegate(typeof(TFunc));
        }

        /// <summary>
        /// Creates open instance delegate of the given type which casts inputs, calls the method delegate and then casts the return type
        /// </summary>
        public static Func<TDesiredTarget, TDesiredReturn> CreateOpenInstanceDelegateForcingType<TDesiredTarget, TDesiredReturn>(this MethodInfo m) =>
            CreateOpenInstanceDelegateForcingType<Func<TDesiredTarget, TDesiredReturn>>(m, nameof(CreateWeaklyTypedDelegateFor0Params));

        private static Func<TDesiredTarget, TDesiredReturn> CreateWeaklyTypedDelegateFor0Params<TTarget, TReturn, TDesiredTarget, TDesiredReturn>(MethodInfo method)
            where TTarget : class, TDesiredTarget
            where TReturn : TDesiredReturn

        {
            var func = method.CreateOpenDelegate<Func<TTarget, TReturn>>();
            return (TDesiredTarget target) => target is TTarget tt ? (TDesiredReturn)(object)func(tt) : default;
        }

        /// <summary>
        /// Creates an open instance delegate of the given type which casts inputs, calls the method delegate and then casts the return type
        /// </summary>
        public static Func<TDesiredTarget, TDesiredParam1, TDesiredReturn> CreateOpenInstanceDelegateForcingType<TDesiredTarget, TDesiredParam1, TDesiredReturn>(this MethodInfo m) => CreateOpenInstanceDelegateForcingType<Func<TDesiredTarget, TDesiredParam1, TDesiredReturn>>(m, nameof(CreateWeaklyTypedDelegateFor1Param));

        private static Func<TDesiredTarget, TDesiredParam, TDesiredReturn> CreateWeaklyTypedDelegateFor1Param<TTarget, TParam, TReturn, TDesiredTarget, TDesiredParam, TDesiredReturn>(MethodInfo method)
            where TTarget : class, TDesiredTarget

        {
            var paramIsReferenceType = default(TParam) == null;
            var func = method.CreateOpenDelegate<Func<TTarget, TParam, TReturn>>();
            return (TDesiredTarget target, TDesiredParam param) => {
                var desiredParam = paramIsReferenceType ? param is TParam p ? p : default : (TParam)(object)param;
                return target is TTarget desiredTarget ? (TDesiredReturn)(object)func(desiredTarget, desiredParam) : default;
            };
        }

        /// <typeparam name="TDesiredFunc">Must be a Func</typeparam>
        private static TDesiredFunc CreateOpenInstanceDelegateForcingType<TDesiredFunc>(this MethodInfo m, string createWeaklyTypedDelegateMethodName) where TDesiredFunc : Delegate
        {
            var desiredFuncType = typeof(TDesiredFunc);
            var genericArgs = new[] { m.DeclaringType }.Concat(m.GetParameters().Select(p => p.ParameterType)).Concat(new[] { m.ReturnType }).Concat(desiredFuncType.GenericTypeArguments).ToArray();
            var createWeaklyTypedDelegateInner = typeof(MethodInfoExtensions)
                .GetMethod(createWeaklyTypedDelegateMethodName, BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(genericArgs)
                .CreateOpenDelegate<Func<MethodInfo, TDesiredFunc>>();
            return createWeaklyTypedDelegateInner(m);
        }

        public static MethodInfo ReflectedPropertyGetter<TInstance>(this TInstance instance,
            string propertyToAccess)
        {
            var propertyInfo = instance.GetType().GetProperty(propertyToAccess, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            return propertyInfo?.GetMethod.GetRuntimeBaseDefinition();
        }
    }
}