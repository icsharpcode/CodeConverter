public void DummyMethod(string target)
{
    if (Operators.CompareString(target, 'Z'.ToString(), false) < 0 || Operators.CompareString(new string(new char[] { }), target, false) <= 0 || string.IsNullOrEmpty(target) || !string.IsNullOrEmpty(target) || Operators.CompareString(target, new string(new char[] { }), false) >= 0 || Operators.CompareString(target, "", false) > 0)
    {
        Console.WriteLine("It must be one of those");
    }
}