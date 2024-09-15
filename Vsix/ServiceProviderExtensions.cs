using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.VsExtension;

internal static class ServiceProviderExtensions
{
    public static async Task<T> GetServiceAsync<T>(this IAsyncServiceProvider provider)
    {
        return (T)(await provider.GetServiceAsync(typeof(T)));
    }
    public static async Task<TReturn> GetServiceAsync<TService, TReturn>(this IAsyncServiceProvider provider)
    {
        return (TReturn)(await provider.GetServiceAsync(typeof(TService)));
    }
}