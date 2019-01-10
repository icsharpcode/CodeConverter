using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeConverter.Web.Models;
using Microsoft.AspNetCore.Mvc;
using ICSharpCode.CodeConverter;
using ICSharpCode.CodeConverter.Shared;

namespace CodeConverter.Web
{
    [Route("api/[controller]")]
    public class ConverterController : Controller
    {
        [HttpPost]
        [Produces(typeof(ConvertResponse))]
        public IActionResult Post([FromBody]ConvertRequest todo)
        {
            var languages = todo.requestedConversion.Split('2');

            string fromLanguage = "C#";
            string toLanguage = "Visual Basic";
            int fromVersion = 6;
            int toVersion = 14;

            if (languages.Length == 2) {
                fromLanguage = ParseLanguage(languages[0]);
                fromVersion = GetDefaultVersionForLanguage(languages[0]);
                toLanguage = ParseLanguage(languages[1]);
                toVersion = GetDefaultVersionForLanguage(languages[1]);
            }

            var codeWithOptions = new CodeWithOptions(todo.code)
                .WithTypeReferences(DefaultReferences.NetStandard2)
                .SetFromLanguage(fromLanguage, fromVersion)
                .SetToLanguage(toLanguage, toVersion);
            var result = ICSharpCode.CodeConverter.CodeConverter.Convert(codeWithOptions);

            var response = new ConvertResponse() {
                conversionOk = result.Success,
                convertedCode = result.ConvertedCode,
                errorMessage = result.GetExceptionsAsString()
            };

            return Ok(response);
        }

        string ParseLanguage(string language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));
            if (language.StartsWith("cs", StringComparison.OrdinalIgnoreCase))
                return "C#";
            if (language.StartsWith("vb", StringComparison.OrdinalIgnoreCase))
                return "Visual Basic";
            throw new ArgumentException($"{language} not supported!");
        }

        int GetDefaultVersionForLanguage(string language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));
            if (language.StartsWith("cs", StringComparison.OrdinalIgnoreCase))
                return 6;
            if (language.StartsWith("vb", StringComparison.OrdinalIgnoreCase))
                return 14;
            throw new ArgumentException($"{language} not supported!");
        }
    }
}
