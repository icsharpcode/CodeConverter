using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using ICSharpCode.CodeConverter;

[WebService(Namespace = "http://converter.telerik.com")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class Service : System.Web.Services.WebService
{
	#region Public Methods
	[WebMethod]
	public string ConvertToVB(string cSharpCode)
	{
		if (string.IsNullOrEmpty(cSharpCode)) {
			return string.Empty;
		}

		var result = CS2VB(cSharpCode);
		return result.Success ? AddFooter(result.ConvertedCode, true) : FormatError(result.GetExceptionsAsString());
	}

	[WebMethod]
	public string ConvertToCS(string vbCode)
	{
		if (string.IsNullOrEmpty(vbCode)) {
			return string.Empty;
		}

		var result = VB2CS(vbCode);
		return result.Success ? AddFooter(result.ConvertedCode, false) : FormatError(result.GetExceptionsAsString());
	}
	#endregion

	private static ConversionResult CS2VB(string cSharpCode)
	{
		var codeWithOptions = new CodeWithOptions(cSharpCode);
		return CodeConverter.Convert(codeWithOptions);
	}

	private static ConversionResult VB2CS(string vbCode)
	{
		var codeWithOptions = new CodeWithOptions(vbCode).SetFromLanguage("Visual Basic", 14).SetToLanguage("C#", 6);
		return CodeConverter.Convert(codeWithOptions);
	}

	private string AddFooter(string result, bool isVB)
	{
		string commentChar = (isVB) ? "'" : "//";

		string strFooter = "\r\n{0}=======================================================\r\n" +
							  "{0}Service provided by Telerik (www.telerik.com)\r\n" +
							  "{0}Conversion powered by Refactoring Essentials.\r\n" +
							  "{0}Twitter: @telerik\r\n" +
							  "{0}Facebook: facebook.com/telerik\r\n" +
							  "{0}=======================================================\r\n";

		return string.Format("\r\n{0}{1}", result, String.Format(strFooter, commentChar));
	}

	private string FormatError(string errMsg)
	{
		//Add friendly text to error message
		return string.Format("\r\nCONVERSION ERROR: Code could not be converted. Details:\r\n\r\n{0}\r\nPlease check for any errors in the original code and try again.\r\n", errMsg);
	}
}