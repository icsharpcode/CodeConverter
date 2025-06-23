using System;
using System.Threading.Tasks;

internal partial class TestClass
{
    public async Task<bool> mySub()
    {
        return await ExecuteAuthenticatedAsync(async () => await DoSomethingAsync());

    }
    private async Task<bool> ExecuteAuthenticatedAsync(Func<Task<bool>> myFunc)
    {
        return await myFunc();
    }
    private async Task<bool> DoSomethingAsync()
    {
        return true;
    }
}