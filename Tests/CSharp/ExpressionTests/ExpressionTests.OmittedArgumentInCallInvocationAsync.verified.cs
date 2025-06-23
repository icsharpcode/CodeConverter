using System;

public partial class Issue445MissingParameter
{
    public void First(string a, string b, int c)
    {
        mySuperFunction(7, optionalSomething: new object());
    }

    private void mySuperFunction(int intSomething, object p = null, object optionalSomething = null)
    {
        throw new NotImplementedException();
    }
}