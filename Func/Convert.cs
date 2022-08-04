using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ICSharpCode.CodeConverter.Func;

public class Convert
{
    private readonly ILoggerFactory _loggerFactory;

    public Convert(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    //
    // Sample data: {"code":"Public Class VisualBasicClass\r\n\r\nEnd Class","requestedConversion":"vbnet2cs"}
    //
    [FunctionName("Convert")]
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods - Name must be "Run" for this to work AFAIK
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        CancellationToken hostCancellationToken)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
    {
        var logger = _loggerFactory.CreateLogger<Convert>();
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<ConvertRequest>(requestBody);

        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken, req.HttpContext.RequestAborted);
        var response = await WebConverter.ConvertAsync(data, cancellationSource.Token);

        return new OkObjectResult(response);
    }
}