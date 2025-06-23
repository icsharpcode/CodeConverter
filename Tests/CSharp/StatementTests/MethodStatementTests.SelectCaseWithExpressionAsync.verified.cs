
public partial class TestClass
{
    public static string TimeAgo(int daysAgo)
    {
        switch (daysAgo)
        {
            case var @case when 0 <= @case && @case <= 3:
            case 4:
            case var case1 when case1 >= 5:
            case var case2 when case2 < 6:
            case var case3 when case3 <= 7:
                {
                    return "this week";
                }
            case var case4 when case4 > 0:
                {
                    return daysAgo / 7 + " weeks ago";
                }

            default:
                {
                    return "in the future";
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code