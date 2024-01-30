using ICSharpCode.CodeConverter.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ICSharpCode.CodeConverter.Func;

public class Version
{
    private readonly ILogger<Version> _logger;

    public Version(ILogger<Version> logger)
    {
        _logger = logger;
    }

    [Function("Version")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        return new OkObjectResult(CodeConverterVersion.GetVersion());
    }
}
