namespace CodeConverter.Tests.Compilation
{
    public interface ICompiler
    {
        CompilerFrontend Compile { get; }
    }
}
