using System.Globalization;
using System.Text;

namespace ICSharpCode.CodeConverter;

public class ConversionResult
{
    private string _sourcePathOrNull;
    private string _targetPathOrNull;
    public bool Success => ConvertedCode != null;
    public string ConvertedCode { get; private set; }
    public IReadOnlyList<string> Exceptions { get; internal set; }
    internal bool IsIdentity { get; set; }

    public string SourcePathOrNull {
        get => _sourcePathOrNull;
        set => _sourcePathOrNull = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public string TargetPathOrNull {
        get => _targetPathOrNull ?? (SourcePathOrNull != null ? PathConverter.TogglePathExtension(SourcePathOrNull) : null);
        set => _targetPathOrNull = value;
    }

    public ConversionResult(string convertedCode = null)
    {
        ConvertedCode = convertedCode;
    }

    public ConversionResult(params Exception[] exceptions)
    {
        Exceptions = exceptions.Select(e => e.ToString()).ToList();
    }

    public string GetExceptionsAsString()
    {
        if (Exceptions == null || Exceptions.Count == 0)
            return String.Empty;

        var builder = new StringBuilder();

        for (int i = 0; i < Exceptions.Count; i++) {
            if (Exceptions.Count > 1) {
                builder.AppendFormat(CultureInfo.InvariantCulture, "----- Exception {0} of {1} -----" + Environment.NewLine, i + 1, Exceptions.Count);
            }
            builder.AppendLine(Exceptions[i]);
        }
        return builder.ToString();
    }

    public void WriteToFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TargetPathOrNull));
        File.WriteAllText(TargetPathOrNull, ConvertedCode ?? GetExceptionsAsString(), Encoding.UTF8);
    }
}