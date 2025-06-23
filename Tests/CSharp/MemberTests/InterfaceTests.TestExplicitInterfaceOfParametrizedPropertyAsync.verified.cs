using System;

internal partial interface IClass
{
    string get_ReadOnlyPropToRename(int i);
    void set_WriteOnlyPropToRename(int i, string value);
    string get_PropToRename(int i);
    void set_PropToRename(int i, string value);

    string get_ReadOnlyPropNonPublic(int i);
    void set_WriteOnlyPropNonPublic(int i, string value);
    string get_PropNonPublic(int i);
    void set_PropNonPublic(int i, string value);

    string get_ReadOnlyPropToRenameNonPublic(int i);
    void set_WriteOnlyPropToRenameNonPublic(int i, string value);
    string get_PropToRenameNonPublic(int i);
    void set_PropToRenameNonPublic(int i, string value);

}

internal partial class ChildClass : IClass
{

    public string get_ReadOnlyPropRenamed(int i)
    {
        throw new NotImplementedException();
    }
    string IClass.get_ReadOnlyPropToRename(int i) => get_ReadOnlyPropRenamed(i);

    public virtual void set_WriteOnlyPropRenamed(int i, string value)
    {
        throw new NotImplementedException();
    }
    void IClass.set_WriteOnlyPropToRename(int i, string value) => set_WriteOnlyPropRenamed(i, value);

    public virtual string get_PropRenamed(int i)
    {
        throw new NotImplementedException();
    }
    public virtual void set_PropRenamed(int i, string value)
    {
        throw new NotImplementedException();
    }

    string IClass.get_PropToRename(int i) => get_PropRenamed(i);
    void IClass.set_PropToRename(int i, string value) => set_PropRenamed(i, value);

    private string get_ReadOnlyPropNonPublic(int i)
    {
        throw new NotImplementedException();
    }
    string IClass.get_ReadOnlyPropNonPublic(int i) => get_ReadOnlyPropNonPublic(i);

    protected internal virtual void set_WriteOnlyPropNonPublic(int i, string value)
    {
        throw new NotImplementedException();
    }
    void IClass.set_WriteOnlyPropNonPublic(int i, string value) => set_WriteOnlyPropNonPublic(i, value);

    internal virtual string get_PropNonPublic(int i)
    {
        throw new NotImplementedException();
    }
    internal virtual void set_PropNonPublic(int i, string value)
    {
        throw new NotImplementedException();
    }

    string IClass.get_PropNonPublic(int i) => get_PropNonPublic(i);
    void IClass.set_PropNonPublic(int i, string value) => set_PropNonPublic(i, value);

    protected internal virtual string get_ReadOnlyPropRenamedNonPublic(int i)
    {
        throw new NotImplementedException();
    }
    string IClass.get_ReadOnlyPropToRenameNonPublic(int i) => get_ReadOnlyPropRenamedNonPublic(i);

    private void set_WriteOnlyPropRenamedNonPublic(int i, string value)
    {
        throw new NotImplementedException();
    }
    void IClass.set_WriteOnlyPropToRenameNonPublic(int i, string value) => set_WriteOnlyPropRenamedNonPublic(i, value);

    internal virtual string get_PropToRenameNonPublic(int i)
    {
        throw new NotImplementedException();
    }
    internal virtual void set_PropToRenameNonPublic(int i, string value)
    {
        throw new NotImplementedException();
    }

    string IClass.get_PropToRenameNonPublic(int i) => get_PropToRenameNonPublic(i);
    void IClass.set_PropToRenameNonPublic(int i, string value) => set_PropToRenameNonPublic(i, value);
}