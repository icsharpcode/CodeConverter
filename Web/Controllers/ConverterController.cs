using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ICSharpCode.CodeConverter.Web
{
    [Route("api/[controller]")]
    public class ConverterController : Controller
    {
        [HttpPost]
        [Produces(typeof(ConvertResponse))]
        public async Task<IActionResult> Post([FromBody] ConvertRequest todo)
        {
            var response = await WebConverter.ConvertAsync(todo);
            return Ok(response);
        }
    }
}