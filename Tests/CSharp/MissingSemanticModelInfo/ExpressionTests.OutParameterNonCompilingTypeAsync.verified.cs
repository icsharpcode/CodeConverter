using System.Collections.Generic;

public partial class OutParameterWithMissingType
{
    private static void AddToDict(Dictionary<int, MissingType> pDict, int pKey)
    {
        MissingType anInstance = default;
        if (!pDict.TryGetValue(pKey, out anInstance))
        {
            anInstance = new MissingType();
            pDict.Add(pKey, anInstance);
        }
    }
}

public partial class OutParameterWithNonCompilingType
{
    private static void AddToDict(Dictionary<OutParameterWithMissingType, MissingType> pDict, OutParameterWithMissingType pKey)
    {
        MissingType anInstance = default;
        if (!pDict.TryGetValue(pKey, out anInstance))
        {
            anInstance = new MissingType();
            pDict.Add(pKey, anInstance);
        }
    }
}
1 source compilation errors:
BC30002: Type 'MissingType' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'MissingType' could not be found (are you missing a using directive or an assembly reference?)