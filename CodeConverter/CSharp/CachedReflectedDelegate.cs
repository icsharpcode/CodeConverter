using System.Diagnostics;

namespace ICSharpCode.CodeConverter.CSharp;

internal class CachedReflectedDelegate<TArg, TResult>
{
    private Func<TArg, TResult> _cachedDelegate;
    private readonly string _propertyName;

    public CachedReflectedDelegate(string propertyName)
    {
        _propertyName = propertyName;
    }

    public TResult GetValue(TArg instance)
    {
        if (_cachedDelegate != null) return _cachedDelegate(instance);

        var getDelegate = instance.ReflectedPropertyGetter(_propertyName)
            ?.CreateOpenInstanceDelegateForcingType<TArg, TResult>();
        if (getDelegate == null) {
            Debug.Fail($"Delegate not found for {instance.GetType()}");
            return default;
        }

        _cachedDelegate = getDelegate;
        return _cachedDelegate(instance);
    }

    public TResult GetValueOrDefault(TArg instance, TResult defaultValue = default)
    {
        try {
            return GetValue(instance);
        } catch (Exception) {
            return defaultValue;
        }
    }
}