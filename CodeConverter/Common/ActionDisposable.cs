﻿namespace ICSharpCode.CodeConverter.Common;

internal sealed class ActionDisposable : IDisposable
{
    private readonly Action _onDispose;

    public ActionDisposable(Action onDispose)
    {
        _onDispose = onDispose;
    }

    public void Dispose() => _onDispose();
}