public void DummyMethod()
{
    int[] someArray = new int[] { 1, 2, 3 };
    for (short index = 0, loopTo = (short)(someArray.Length - 1); index <= loopTo; index++)
        Console.WriteLine(index);
}