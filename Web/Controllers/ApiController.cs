using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ICSharpCode.CodeConverter.Web.Controllers
{
    [Route("api")]
    public class ApiController : Controller
    {
        [HttpPost]
        [Route("Convert")]
        [Route("Converter")] //Old name, kept for compatibility
        [Produces(typeof(ConvertResponse))]
        public async Task<IActionResult> PostAsync([FromBody] ConvertRequest todo) => Ok(await WebConverter.ConvertAsync(todo));

        [HttpGet]
        [Route("Version")]
        [Produces(typeof(string))]
        public async Task<IActionResult> GetAsync() => Ok(CodeConverterVersion.GetVersion());
    }
}