﻿using System;
using System.Threading;

namespace ICSharpCode.CodeConverter.VsExtension;

internal sealed class Cancellation : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private CancellationTokenSource _commandCancellationTokenSource = new();

    public CancellationTokenSource ResetCommandCancellation()
    {
        _commandCancellationTokenSource.Cancel();
        _commandCancellationTokenSource.Dispose();
        return _commandCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
    }

    public CancellationToken CancelAll => _cancellationTokenSource.Token;

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
    }
}