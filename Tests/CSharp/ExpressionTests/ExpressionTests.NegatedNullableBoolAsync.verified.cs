
public enum CrashEnum
{
    None = 0,
    One = 1,
    Two = 2
}

public partial class CrashClass
{
    public CrashEnum? CrashEnum { get; set; }
    public bool IsSet { get; set; }
}

public partial class CrashTest
{
    public object Edit(bool flag2 = false, CrashEnum? crashEnum = default)
    {
        CrashClass CrashClass = null;
        bool Flag0 = true;
        bool Flag1 = true;
        if (Flag0)
        {
            if (Flag1 && flag2)
            {
                if ((int)crashEnum.GetValueOrDefault() > 0 && (!CrashClass.CrashEnum.HasValue ? true : CrashClass.CrashEnum is var arg1 && crashEnum.HasValue && arg1.HasValue ? crashEnum.Value != arg1.Value : (bool?)null).GetValueOrDefault())
                {
                    CrashClass.CrashEnum = crashEnum;
                    CrashClass.IsSet = true;
                }
            }
        }
        return null;
    }
}