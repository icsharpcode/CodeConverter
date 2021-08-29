using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.AspNetCore.Mvc;

namespace ICSharpCode.CodeConverter.Web.Controllers
{
    [Route("api/[controller]")]
    public class VersionController : Controller
    {
        [HttpGet]
        [Produces(typeof(string))]
        public async Task<IActionResult> GetAsync() => Ok(CodeConverterVersion.GetVersion());
    }
}