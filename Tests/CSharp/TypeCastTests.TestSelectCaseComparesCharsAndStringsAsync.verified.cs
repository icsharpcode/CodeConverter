
internal partial class CharTestClass
{
    private void Q()
    {
        switch ("a")
        {
            case var @case when "x" <= @case && @case <= "y":
                {
                    break;
                }

            case "b":
                {
                    break;
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code