public void Main()
{
    float foo = 3.5f;
    int index;
    var loopTo = (int)Math.Round(Conversion.Int(foo * 3f));
    for (index = (int)Math.Round(Conversion.Int(foo)); index <= loopTo; index++)
        Console.WriteLine(index);
}