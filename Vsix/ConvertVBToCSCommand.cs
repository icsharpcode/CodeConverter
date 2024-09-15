using Microsoft.VisualStudio.Shell;

namespace ICSharpCode.CodeConverter.VsExtension;

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Commands;

public abstract class ConvertCommandBase : Command
{
    protected readonly CodeConversion _codeConversion;
    protected readonly CodeConverterPackage _package;
    protected const string ProjectExtension = ".vbproj";

    protected ConvertCommandBase(CodeConverterPackage package, CodeConversion codeConversion)
    {
        _package = package ?? throw new ArgumentNullException(nameof(package));
        _codeConversion = codeConversion;
    }

    protected IAsyncServiceProvider ServiceProvider => _package;

    protected async Task ShowExceptionAsync(Exception ex)
    {
        await _package.ShowExceptionAsync(ex);
    }
}