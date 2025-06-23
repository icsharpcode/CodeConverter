{
    var anonymousType1 = new
    {
        A = 1 is var tempA ? tempA : default,
        B = tempA
    };
    var anonymousType2 = new
    {
        A = 2 is var tempA1 ? tempA1 : default,
        B = tempA1
    };
}