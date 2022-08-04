using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(ICSharpCode.CodeConverter.Func.Startup))]

namespace ICSharpCode.CodeConverter.Func
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            //builder.Services.AddHttpClient();

            //builder.Services.AddSingleton<IMyService>((s) => {
            //    return new MyService();
            //});

            //builder.Services.AddSingleton<ILoggerProvider, MyLoggerProvider>();
        }
    }
}