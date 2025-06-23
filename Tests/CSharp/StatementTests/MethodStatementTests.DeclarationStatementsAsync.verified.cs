using System;

internal partial class Test
{
    private void TestMethod()
    {
    the_beginning:
        ;

        int value = 1;
        const double myPIe = 2d * Math.PI;
        string text = "This is my text!";
        goto the_beginning;
    }
}