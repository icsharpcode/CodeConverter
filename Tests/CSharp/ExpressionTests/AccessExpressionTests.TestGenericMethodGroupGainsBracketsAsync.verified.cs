using System;
using System.Collections.Generic;
using System.Linq;

public enum TheType
{
    Tree
}

public partial class MoreParsing
{
    public void DoGet()
    {
        var anon = new { TheType = GetEnumValues<TheType>() };
    }

    private IDictionary<int, string> GetEnumValues<TEnum>()
    {
        return Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToDictionary(enumValue => (int)(object)enumValue, enumValue => enumValue.ToString());
    }
}