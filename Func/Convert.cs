using ICSharpCode.CodeConverter.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ICSharpCode.CodeConverter.Func;

public class Convert
{
    public const string DefaultRequest = @"{""code"":""Public Class VisualBasicClass\r\n\r\nEnd Class"",""requestedConversion"":""vbnet2cs""}";
    public const string DefaultConversion = "\r\npublic partial class VisualBasicClass\r\n{\r\n\r\n}";

    private readonly ILogger<Convert> _logger;

    public Convert(ILogger<Convert> logger)
    {
        _logger = logger;
    }

    //
    // Sample data: {"code":"Public Class VisualBasicClass\r\n\r\nEnd Class","requestedConversion":"vbnet2cs"}
    //
    [Function("Convert")]
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        CancellationToken hostCancellationToken)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        if (0 == string.CompareOrdinal(requestBody, DefaultRequest)) {
            _logger.LogInformation("Short-circuiting for default conversion request");
            return new OkObjectResult(new ConvertResponse(true, DefaultConversion, ""));
        }

        var data = JsonConvert.DeserializeObject<ConvertRequest>(requestBody);

        if (null == data) {
            return new BadRequestResult();
        }

        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken, req.HttpContext.RequestAborted);
        var response = await WebConverter.ConvertAsync(data, cancellationSource.Token);

        return new OkObjectResult(response);
    }
}
