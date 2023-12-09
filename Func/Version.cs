using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace ICSharpCode.CodeConverter.Func;

public class Version
{
    private readonly ILogger _logger;

    public Version(ILogger<Version> logger)
    {
        _logger = logger;
    }

    [Function("Version")]
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods - Name must be "Run" for this to work AFAIK
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req) =>
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        new OkObjectResult(CodeConverterVersion.GetVersion());
}