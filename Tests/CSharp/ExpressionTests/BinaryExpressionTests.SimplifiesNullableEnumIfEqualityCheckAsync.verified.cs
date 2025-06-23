using System;

public enum PasswordStatus
{
    Expired,
    Locked
}

public partial class TestForEnums
{
    public static void WriteStatus(PasswordStatus? status)
    {
        if (status.HasValue && status.Value == PasswordStatus.Locked)
        {
            Console.Write("Locked");
        }
    }
}