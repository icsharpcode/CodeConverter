using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.VsExtension
{
    internal static class TaskExtensions {
        public static void ForgetNoThrow(this Task task)
        {
            var unused = task.NoThrowAwaitable();
        }
    }
}
