using System.Globalization;

internal partial class Issue579SelectCaseWithCaseInsensitiveTextCompare
{
    private bool? Test(string astr_Temp)
    {
        switch (astr_Temp ?? "")
        {
            case var @case when CultureInfo.CurrentCulture.CompareInfo.Compare(@case, "Test", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0:
                {
                    return true;
                }
            case var case1 when CultureInfo.CurrentCulture.CompareInfo.Compare(case1, astr_Temp ?? "", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0:
                {
                    return false;
                }

            default:
                {
                    return default;
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code