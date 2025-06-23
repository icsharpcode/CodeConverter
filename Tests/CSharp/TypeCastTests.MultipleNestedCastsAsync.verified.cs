using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class MultipleCasts
{
    public static T ToGenericParameter<T>(object Value)
    {
        if (Value is null)
        {
            return default;
        }
        var reflectedType = typeof(T);
        if (Equals(reflectedType, typeof(short)))
        {
            return (T)(object)Conversions.ToShort(Value);
        }
        else if (Equals(reflectedType, typeof(ulong)))
        {
            return (T)(object)Conversions.ToULong(Value);
        }
        else
        {
            return (T)Value;
        }
    }
}