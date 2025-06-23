
public void S()
{
    var a = default(object);
    var b = default(object);
    if (Conversions.ToBoolean(Operators.AndObject(Operators.AndObject(1 == int.Parse("1"), b), Operators.ConditionalCompareObjectEqual(a, 1, false))))
    {
    }
    else if (Conversions.ToBoolean(Operators.OrObject(Operators.OrObject(Operators.OrObject(Operators.ConditionalCompareObjectEqual(a, 1, false), b), Operators.ConditionalCompareObjectEqual(a, 2, false)), Operators.ConditionalCompareObjectEqual(a, 3, false))))
    {
    }
}