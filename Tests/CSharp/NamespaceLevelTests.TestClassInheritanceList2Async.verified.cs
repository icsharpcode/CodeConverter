using System;

internal abstract partial class ClassA : EventArgs, IDisposable
{

    protected abstract void Test();
    public abstract void Dispose();
}