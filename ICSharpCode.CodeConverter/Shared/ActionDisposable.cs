using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{

    internal sealed class ActionDisposable : IDisposable
    {
        private readonly Action _onDispose;

        public ActionDisposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose() => _onDispose();
    }
}