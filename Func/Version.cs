using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ICSharpCode.CodeConverter.Func;

public static class Version
{
    [FunctionName("Version")]
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods - Name must be "Run" for this to work AFAIK
    public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log) =>
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        new OkObjectResult(CodeConverterVersion.GetVersion());
}