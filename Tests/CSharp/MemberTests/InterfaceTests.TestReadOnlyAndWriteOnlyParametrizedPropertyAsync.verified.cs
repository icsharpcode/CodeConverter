using System;

internal partial interface IClass
{
    string get_ReadOnlyProp(int i);
    void set_WriteOnlyProp(int i, string value);
}

internal partial class ChildClass : IClass
{

    public virtual string get_ReadOnlyProp(int i)
    {
        throw new NotImplementedException();
    }

    public virtual void set_WriteOnlyProp(int i, string value)
    {
        throw new NotImplementedException();
    }
}