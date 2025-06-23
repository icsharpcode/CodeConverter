using System.Threading.Tasks;

internal partial class AsyncCode
{
    public void NotAsync()
    {
        async Task<int> a1() => 3;
        async Task<int> a2() => await Task.FromResult(3);
        async void a3() => await Task.CompletedTask;
        async void a4() => await Task.CompletedTask;
    }

    public async Task<int> AsyncFunc()
    {
        return await Task.FromResult(3);
    }
    public async void AsyncSub()
    {
        await Task.CompletedTask;
    }
}
