namespace ICSharpCode.CodeConverter.Web.Models
{
    public class ConvertResponse
    {
        public bool conversionOk { get; set; }
        public string convertedCode { get; set; }
        public string errorMessage { get; set; }
    }
}
