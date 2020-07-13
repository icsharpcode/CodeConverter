using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Web.Models;
using Microsoft.AspNetCore.Mvc;
using ICSharpCode.CodeConverter;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Web
{
    [Route("api/[controller]")]
    public class ConverterController : Controller
    {
        [HttpPost]
        [Produces(typeof(ConvertResponse))]
        public async Task<IActionResult> Post([FromBody]ConvertRequest todo)
        {
            var languages = todo.requestedConversion.Split('2');

            string fromLanguage = LanguageNames.CSharp;
            string toLanguage = LanguageNames.VisualBasic;
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
            var result = await CodeConverter.ConvertAsync(codeWithOptions);

            var response = new ConvertResponse() {
                conversionOk = result.Success,
                convertedCode = result.ConvertedCode,
                errorMessage = result.GetExceptionsAsString()
            };

            return Ok(response);
        }

        private string ParseLanguage(string language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));
            if (language.StartsWith("cs", StringComparison.OrdinalIgnoreCase))
                return LanguageNames.CSharp;
            if (language.StartsWith("vb", StringComparison.OrdinalIgnoreCase))
                return LanguageNames.VisualBasic;
            throw new ArgumentException($"{language} not supported!");
        }

        private int GetDefaultVersionForLanguage(string language)
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
