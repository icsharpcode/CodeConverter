using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.CodeConverter.Shared
{
    /// <summary>
    /// https://github.com/icsharpcode/CodeConverter/issues/598#issuecomment-663773878
    /// </summary>
    public class OptionalOperations
    {
        private readonly IProgress<ConversionProgress> _progress;
        private readonly CancellationToken _wholeTaskCancellationToken;
        private readonly ActivityMonitor _activityMonitor;
        private readonly CancellationTokenSource _optionalTaskCts;

        public OptionalOperations(TimeSpan abandonTasksIfNoActivityFor, IProgress<ConversionProgress> progress,
            CancellationToken wholeTaskCancellationToken)
        {
            _progress = progress;
            _wholeTaskCancellationToken = wholeTaskCancellationToken;
            _optionalTaskCts = CancellationTokenSource.CreateLinkedTokenSource(wholeTaskCancellationToken);
            _activityMonitor = new ActivityMonitor(abandonTasksIfNoActivityFor, _optionalTaskCts, () => 
                _progress.Report(new ConversionProgress("WARNING: Skipping all further formatting, you can increase the timeout for this in Tools -> Options -> Code Converter."))
            );
        }

        public SyntaxNode MapSourceTriviaToTargetHandled<TSource, TTarget>(TSource root,
            TTarget converted, Document document)
            where TSource : SyntaxNode, ICompilationUnitSyntax where TTarget : SyntaxNode, ICompilationUnitSyntax
        {
            try
            {
                converted = (TTarget) Format(converted, document);
                return LineTriviaMapper.MapSourceTriviaToTarget(root, converted);
            }
            catch (Exception e)
            {
                _progress.Report(new ConversionProgress($"Error while formatting and converting comments: {e}"));
                return converted;
            }
        }

        public SyntaxNode Format(SyntaxNode node, Document document)
        {
            if (!_optionalTaskCts.IsCancellationRequested) {
                try {
                    _optionalTaskCts.Token.ThrowIfCancellationRequested();
                    _activityMonitor.ActivityStarted();
                    // This call is very expensive for large documents. Should look for a more performant version, e.g. Is NormalizeWhitespace good enough?
                    return Formatter.Format(node, document.Project.Solution.Workspace,
                        cancellationToken: _optionalTaskCts.Token);

                } catch (OperationCanceledException) {
                } finally {
                    _activityMonitor.ActivityFinished();
                }
            }
            _progress.Report(new ConversionProgress("Skipped formatting", 1));
            return node.NormalizeWhitespace();
        }

        /// <summary>
        /// Reasonably lightweight check that there's been some activity within the given timeout.
        /// </summary>
        private class ActivityMonitor
        {
            private readonly TimeSpan _timeout;
            private readonly CancellationTokenSource _cts;
            private readonly Action _afterInactivity;
            private volatile int _activeOperations;
            /// <summary>
            /// Must check <see cref="_activeOperations"/> within the lock before changed timer.Enabled
            /// This avoids race conditions between the last task of a set finishing and the first of a new set starting
            /// </summary>
            private readonly object _timerEnabledWriteLock = new object();
            private static Timer _timer;


            private void OnTimedEvent(object state)
            {
                if (!_cts.IsCancellationRequested && _activeOperations > 0) {
                    _cts.Cancel();
                    _afterInactivity();
                } else if (_activeOperations > 0) {
                    ActivityObserved();
                }
            }

            public ActivityMonitor(TimeSpan timeout, CancellationTokenSource cts, Action afterInactivity)
            {
                _timeout = timeout;
                _cts = cts;
                _afterInactivity = afterInactivity;
                _timer = new Timer(OnTimedEvent, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }

            public void ActivityStarted()
            {
                if (Interlocked.Increment(ref _activeOperations) == 1) {
                    lock (_timerEnabledWriteLock) {
                        if (_activeOperations > 0) {
                            ActivityObserved();
                        }
                    }
                }
                ActivityObserved();
            }

            public void ActivityFinished()
            {
                ActivityObserved();
                if (Interlocked.Decrement(ref _activeOperations) == 0) {
                    lock (_timerEnabledWriteLock) {
                        if (_activeOperations == 0) {
                            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                        }
                    }
                }
            }

            private void ActivityObserved()
            {
                _timer.Change(_timeout, Timeout.InfiniteTimeSpan);
            }
        }
    }
}