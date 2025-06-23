
internal partial interface IClass
{
    int get_ReadOnlyPropParam(int i);
    int ReadOnlyProp { get; }

    void set_WriteOnlyPropParam(int i, int value);
    int WriteOnlyProp { set; }
}

internal partial class ChildClass : IClass
{

    public virtual int get_RenamedPropertyParam(int i)
    {
        return 1;
    }
    public virtual void set_RenamedPropertyParam(int i, int value)
    {
    }
    int IClass.get_ReadOnlyPropParam(int i) => get_RenamedPropertyParam(i);

    public virtual int RenamedReadOnlyProperty
    {
        get
        {
            return 2;
        }
        set
        {
        }
    }

    int IClass.ReadOnlyProp { get => RenamedReadOnlyProperty; } // Comment moves because this line gets split

    public virtual int get_RenamedWriteOnlyPropParam(int i)
    {
        return 1;
    }
    public virtual void set_RenamedWriteOnlyPropParam(int i, int value)
    {
    }
    void IClass.set_WriteOnlyPropParam(int i, int value) => set_RenamedWriteOnlyPropParam(i, value);

    public virtual int RenamedWriteOnlyProperty
    {
        get
        {
            return 2;
        }
        set
        {
        }
    }

    int IClass.WriteOnlyProp { set => RenamedWriteOnlyProperty = value; } // Comment moves because this line gets split
}