using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CodeConverter.VsExtension
{
    internal class BlockingOleMenuCommand
    {
        private readonly OleMenuCommand _command;

        private static EventHandler WaitFor(Func<object, EventArgs, Task> task)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return (o, a) => ThreadHelper.JoinableTaskFactory.Run(() => task(o,a));
        }

        public BlockingOleMenuCommand(Func<object, EventArgs, Task> invokeHandler, CommandID id)
        {
            _command = new OleMenuCommand(WaitFor(invokeHandler), id);
        }

        public event Func<object, EventArgs, Task> BeforeQueryStatus {
            add => _command.BeforeQueryStatus += WaitFor(value);
            remove => _command.BeforeQueryStatus -= WaitFor(value);
        }

        public static implicit operator OleMenuCommand(BlockingOleMenuCommand blockingCommand)
        {
            return blockingCommand._command;
        }
    }
}