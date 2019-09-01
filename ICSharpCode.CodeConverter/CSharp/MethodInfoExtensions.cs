using System;
using System.Linq;
using System.Reflection;
using System.Text;
using ICSharpCode.CodeConverter.Util;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// Inspired by: https://codeblog.jonskeet.uk/2008/08/09/making-reflection-fly-and-exploring-delegates/
    /// </summary>
    public static class MethodInfoExtensions
    {
        public static Func<TDesiredTarget, TDesiredReturn> CreateOpenDelegateOfType<TDesiredTarget, TDesiredReturn>(this MethodInfo m) where TDesiredTarget : class where TDesiredReturn: class
        {
            var helperMethodInfo = typeof(MethodInfoExtensions)
                .GetMethod(nameof(CreateWeaklyTypedDelegateInner), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(m.DeclaringType, m.ReturnType, typeof(TDesiredTarget), typeof(TDesiredReturn));

            var createWeaklyTypedDelegateInner =
                (Func<MethodInfo, Func<TDesiredTarget, TDesiredReturn>>)helperMethodInfo
                    .CreateDelegate(typeof(Func<MethodInfo, Func<TDesiredTarget, TDesiredReturn>>));

            return createWeaklyTypedDelegateInner(m);
        }

        private static Func<TDesiredTarget, TDesiredReturn> CreateWeaklyTypedDelegateInner<TTarget, TReturn, TDesiredTarget, TDesiredReturn>(MethodInfo method) 
            where TTarget : class, TDesiredTarget
            where TReturn : class, TDesiredReturn

        {
            // Convert the slow MethodInfo into a fast, strongly typed, open delegate
            var func = (Func<TTarget, TReturn>)method.CreateDelegate(typeof(Func<TTarget, TReturn>));

            // Now create a more weakly typed delegate which will call the strongly typed one
            return (TDesiredTarget target) => target is TTarget t1 ? func(t1) : null;
        }
    }
}