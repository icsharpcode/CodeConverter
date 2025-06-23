
public partial class Issue1147
{
    private const uint LargeUInt = 0xFFFFFFFEU;
    private const ulong LargeULong = 0xFFFFFFFFFFFFFFFEUL;
    private const int LargeInt = unchecked((int)0xFFFFFFFE);
    private const long LargeLong = unchecked((long)0xFFFFFFFFFFFFFFFE);
}