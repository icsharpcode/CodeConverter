using System;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class RoslynCrashPreventer
    {
        private static readonly DelegatingHandler _codeAnalysisHandler= new DelegatingHandler(typeof(Compilation).Assembly, "Microsoft.CodeAnalysis.FatalError");
        private static readonly DelegatingHandler _errorReportingHandler = new DelegatingHandler(typeof(WorkspaceDiagnostic).Assembly, "Microsoft.CodeAnalysis.ErrorReporting.FatalError");
        private static volatile int _currentUses;

        /// <summary>
        /// Use this to stop the library exiting the process without telling us.
        /// https://github.com/dotnet/roslyn/issues/41724
        /// </summary>
        /// <remarks>
        /// The simplification code in particular is quite buggy, scattered with "throw ExceptionUtilities.Unreachable" with no particular reasoning for why the code wouldn't be reachable.
        /// It then uses FatalError.ReportUnlessCanceled rather than FatalError.ReportWithoutCrashUnlessCanceled causing fatal crashes with Environment.FailFast
        /// While this presumably allows them to get good low-level debugging info from the windows error reports caused, it just means that people come to this project complaining about VS crashes.
        /// See https://github.com/icsharpcode/CodeConverter/issues/521 and https://github.com/icsharpcode/CodeConverter/issues/484
        /// There are other ways to find these bugs - just run the expander/reducer on a couple of whole open source projects and the bugs will pile up.
        /// </remarks>
        public static IDisposable Create(Action<Exception> logErrorToUseIfNotSet)
        {
            _codeAnalysisHandler.TryExchangeHandler(logErrorToUseIfNotSet);
            _errorReportingHandler.TryExchangeHandler(logErrorToUseIfNotSet);
            Interlocked.Increment(ref _currentUses);
            return new ActionDisposable(() => Interlocked.Decrement(ref _currentUses));
        }

        private class DelegatingHandler
        {
            private readonly Assembly _assembly;
            private readonly string _containingType;
            private readonly object _exchangeLock = new object();
            private Action<Exception> _originalHandler;

            public DelegatingHandler(Assembly assembly, string containingType)
            {
                _assembly = assembly;
                _containingType = containingType;
            }

            public void TryExchangeHandler(Action<Exception> logError)
            {
                if (_originalHandler != null || logError == null) return;
                lock (_exchangeLock) {
                    _originalHandler ??= ExchangeFatalErrorHandler(WrappedCodeAnalysisErrorHandler(logError));
                }
            }

            private Action<Exception> ExchangeFatalErrorHandler(Action<Exception> errorHandler)
            {
                try {
                    var fataErrorType = _assembly.GetType(_containingType);
                    var fatalHandlerField =
                        fataErrorType.GetField("s_fatalHandler", BindingFlags.NonPublic | BindingFlags.Static);
                    var originalHandler = (Action<Exception>)fatalHandlerField.GetValue(null);
                    if (originalHandler != null) {
                        fatalHandlerField.SetValue(null, errorHandler);
                    }

                    return originalHandler;
                } catch (Exception) {
                    return null;
                }
            }

            private Action<Exception> WrappedCodeAnalysisErrorHandler(Action<Exception> errorHandler) => e => {
                if (_currentUses > 0) {
                    errorHandler(e);
                } else {
                    _originalHandler(e);
                }
            };
        }
    }
}