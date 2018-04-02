using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Http.Description;
using CodeConverterWebApp.Models;
using ICSharpCode.CodeConverter;

namespace CodeConverterWebApp.Controllers
{
	public class ConverterController : ApiController
	{
		[HttpPost]
		[ResponseType(typeof(ConvertResponse))]
		public IHttpActionResult Post([FromBody]ConvertRequest todo)
		{
			this.ValidateRequestHeader(this.Request);

			var languages = todo.requestedConversion.Split('2');

			string fromLanguage = "C#";
			string toLanguage = "Visual Basic";
			int fromVersion = 6;
			int toVersion = 14;

			if (languages.Length == 2)
			{
				fromLanguage = ParseLanguage(languages[0]);
				fromVersion = GetDefaultVersionForLanguage(languages[0]);
				toLanguage = ParseLanguage(languages[1]);
				toVersion = GetDefaultVersionForLanguage(languages[1]);
			}

			var codeWithOptions = new CodeWithOptions(todo.code)
				.WithDefaultReferences()
				.SetFromLanguage(fromLanguage, fromVersion)
				.SetToLanguage(toLanguage, toVersion);
			var result = CodeConverter.Convert(codeWithOptions);

			var response = new ConvertResponse()
			{
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

		void ValidateRequestHeader(HttpRequestMessage request)
		{
			string cookieToken = "";
			string formToken = "";

			IEnumerable<string> tokenHeaders;
			if (request.Headers.TryGetValues("RequestVerificationToken", out tokenHeaders)) {
				string[] tokens = tokenHeaders.First().Split(':');
				if (tokens.Length == 2) {
					cookieToken = tokens[0].Trim();
					formToken = tokens[1].Trim();
				}
			}

			AntiForgery.Validate(cookieToken, formToken);
		}
	}
}
