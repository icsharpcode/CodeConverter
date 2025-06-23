{
    var anonymousType = new
    {
        A = 1, // Comment gets duplicated
               // Comment gets duplicated
        B = new
        {
            A = 2 is var tempA ? tempA : default,
            B = tempA
        }
    };
}