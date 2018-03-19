using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.CodeConverter
{
    public class ConversionResult
    {
        private string _sourcePathOrNull;
        public bool Success { get; private set; }
        public string ConvertedCode { get; private set; }
        public IReadOnlyList<Exception> Exceptions { get; private set; }

        public string SourcePathOrNull {
            get => _sourcePathOrNull;
            set => _sourcePathOrNull = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public ConversionResult(string convertedCode, params Exception[] exceptions)
        {
            Success = true;
            ConvertedCode = convertedCode;
            Exceptions = exceptions;
        }

        public ConversionResult(params Exception[] exceptions)
        {
            Success = exceptions.Length == 0;
            Exceptions = exceptions;
        }

        public string GetExceptionsAsString()
        {
            if (Exceptions == null || Exceptions.Count == 0)
                return String.Empty;

            var builder = new StringBuilder();
            if (SourcePathOrNull != null) {
                builder.AppendLine($"In '{SourcePathOrNull}':");
            }
            for (int i = 0; i < Exceptions.Count; i++) {
                if (Exceptions.Count > 1) {
                    builder.AppendFormat("----- Exception {0} of {1} -----" + Environment.NewLine, i + 1, Exceptions.Count);
                }
                builder.AppendLine(Exceptions[i].ToString());
            }
            return builder.ToString();
        }
    }
}
