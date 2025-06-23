using System.Data;

public partial class NonStringSelect
{
    private object Test3(DataRow CurRow)
    {
        foreach (DataColumn CurCol in CurRow.GetColumnsInError())
        {
            switch (CurCol.DataType)
            {
                case var @case when @case == typeof(string):
                    {
                        return false;
                    }

                default:
                    {
                        return true;
                    }
            }
        }

        return default;
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code