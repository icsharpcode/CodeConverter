using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ICSharpCode.CodeConverter.Func
{
    public static class Version
    {
        private static string VersionInfo = "";

        [FunctionName("Version")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string versionInfo = "";
            if (0 == VersionInfo?.Length) {
                Assembly assembly = Assembly.GetAssembly(typeof(CodeConverter));
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

                versionInfo = fvi.FileVersion;
                VersionInfo = versionInfo;
            }

            return new OkObjectResult(versionInfo);
        }
    }
}
