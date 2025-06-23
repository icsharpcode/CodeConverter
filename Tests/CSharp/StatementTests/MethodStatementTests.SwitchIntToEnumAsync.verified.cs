
internal static partial class Main
{
    public enum EWhere : short
    {
        None = 0,
        Bottom = 1
    }

    internal static string prtWhere(EWhere aWhere)
    {
        switch (aWhere)
        {
            case EWhere.None:
                {
                    return " ";
                }
            case EWhere.Bottom:
                {
                    return "_ ";
                }
        }

        return default;

    }
}