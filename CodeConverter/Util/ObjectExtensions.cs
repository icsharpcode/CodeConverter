using System;

namespace ICSharpCode.CodeConverter.Util
{
    [Obsolete("Please use CSharp switch expression instead")]
    internal static partial class ObjectExtensions
    {
        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TBaseType, TResult> defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
        {
            switch (obj) {
                case TDerivedType1 tObj:
                    return matchFunc1(tObj);
                case TDerivedType2 tObj:
                    return matchFunc2(tObj);
                case { } tObj when (defaultFunc != null):
                    return defaultFunc(tObj);
                default:
                    return default(TResult);
            }
        }

        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TBaseType, TResult> defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
            where TDerivedType3 : TBaseType
        {
            switch (obj) {
                case TDerivedType1 tObj:
                    return matchFunc1(tObj);
                case TDerivedType2 tObj:
                    return matchFunc2(tObj);
                case TDerivedType3 tObj:
                    return matchFunc3(tObj);
                case { } tObj when (defaultFunc != null):
                    return defaultFunc(tObj);
                default:
                    return default(TResult);
            }
        }

        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TBaseType, TResult> defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
            where TDerivedType3 : TBaseType
            where TDerivedType4 : TBaseType
        {
            if (obj is TDerivedType1) {
                return matchFunc1((TDerivedType1)obj);
            } else if (obj is TDerivedType2) {
                return matchFunc2((TDerivedType2)obj);
            } else if (obj is TDerivedType3) {
                return matchFunc3((TDerivedType3)obj);
            } else if (obj is TDerivedType4) {
                return matchFunc4((TDerivedType4)obj);
            } else if (defaultFunc != null) {
                return defaultFunc(obj);
            } else {
                return default(TResult);
            }
        }

        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TBaseType, TResult> defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
            where TDerivedType3 : TBaseType
            where TDerivedType4 : TBaseType
            where TDerivedType5 : TBaseType
        {
            if (obj is TDerivedType1) {
                return matchFunc1((TDerivedType1)obj);
            } else if (obj is TDerivedType2) {
                return matchFunc2((TDerivedType2)obj);
            } else if (obj is TDerivedType3) {
                return matchFunc3((TDerivedType3)obj);
            } else if (obj is TDerivedType4) {
                return matchFunc4((TDerivedType4)obj);
            } else if (obj is TDerivedType5) {
                return matchFunc5((TDerivedType5)obj);
            } else if (defaultFunc != null) {
                return defaultFunc(obj);
            } else {
                return default(TResult);
            }
        }
    }
}
