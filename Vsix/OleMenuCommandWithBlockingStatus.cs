using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace CodeConverter.VsExtension
{
    internal class OleMenuCommandWithBlockingStatus
    {
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly OleMenuCommand _command;
        
        public OleMenuCommandWithBlockingStatus(JoinableTaskFactory joinableTaskFactory, Func<object, EventArgs, Task> callbackAsync, CommandID menuCommandId)
        {
            _joinableTaskFactory = joinableTaskFactory;
            _command = new OleMenuCommand(Execute, menuCommandId);

            void Execute(object sender, EventArgs eventArgs)
            {
                async Task ExecuteAsync()
                {
                    await TaskScheduler.Default;
                    await callbackAsync(sender, eventArgs);
                }
                _joinableTaskFactory.RunAsync(ExecuteAsync).Task.Forget();
            }
        }

        public event Func<object, EventArgs, Task> BeforeQueryStatus {
            add => _command.BeforeQueryStatus += WaitFor(value);
            remove => _command.BeforeQueryStatus -= WaitFor(value);
        }

        public static implicit operator OleMenuCommand(OleMenuCommandWithBlockingStatus oleMenuCommandWithBlockingCommand)
        {
            return oleMenuCommandWithBlockingCommand._command;
        }
        private EventHandler WaitFor(Func<object, EventArgs, Task> task)
        {
            return (o, a) => {
                _joinableTaskFactory.Run(() => task(o, a));
            };
        }
    }
}