using System;
using Microsoft.VisualBasic.CompilerServices;
using System.Globalization;

class Program
{
    static void Main()
    {
        char testChar = default;
        Console.WriteLine(CultureInfo.CurrentCulture.CompareInfo.Compare(Conversions.ToString(testChar), "", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0);
        Console.WriteLine(CultureInfo.CurrentCulture.CompareInfo.Compare(Conversions.ToString(testChar), "a", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0);
        Console.WriteLine(CultureInfo.CurrentCulture.CompareInfo.Compare("a", Conversions.ToString(testChar), CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0);
        Console.WriteLine(CultureInfo.CurrentCulture.CompareInfo.Compare("ab", Conversions.ToString(testChar), CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0);

        char c = 'a';
        Console.WriteLine(CultureInfo.CurrentCulture.CompareInfo.Compare(Conversions.ToString(c), "a", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0);
        Console.WriteLine(CultureInfo.CurrentCulture.CompareInfo.Compare(Conversions.ToString(c), "ab", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0);

        string s = "";
        Console.WriteLine(CultureInfo.CurrentCulture.CompareInfo.Compare(Conversions.ToString(c), s, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0);
        Console.WriteLine(CultureInfo.CurrentCulture.CompareInfo.Compare(Conversions.ToString(testChar), s, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0);
    }
}
