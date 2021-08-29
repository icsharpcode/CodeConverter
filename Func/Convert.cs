using System.IO;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Web;
using ICSharpCode.CodeConverter.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ICSharpCode.CodeConverter.Func
{
    public static class Convert
    {
        //
        // Sample data: {"code":"Public Class VisualBasicClass\r\n\r\nEnd Class","requestedConversion":"vbnet2cs"}
        //
        [FunctionName("Convert")]
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods - Name must be "Run" for this to work AFAIK
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<ConvertRequest>(requestBody);

            var response = await WebConverter.ConvertAsync(data);

            return new OkObjectResult(response);
        }
    }
}
