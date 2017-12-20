using System;
using System.Collections.Generic;
using System.Text;

namespace RefactoringEssentials.Converter
{
	public class ConversionResult
	{
		public bool Success { get; private set; }
		public string ConvertedCode { get; private set; }
		public IReadOnlyList<Exception> Exceptions { get; private set; }

		public ConversionResult(string convertedCode)
		{
			Success = !string.IsNullOrWhiteSpace(convertedCode);
			ConvertedCode = convertedCode;
		}

		public ConversionResult(params Exception[] exceptions)
		{
			Success = exceptions.Length == 0;
			Exceptions = exceptions;
		}

		public string GetExceptionsAsString()
		{
			if (Exceptions == null || Exceptions.Count == 0)
				return string.Empty;

			var builder = new StringBuilder();
			for (int i = 0; i < Exceptions.Count; i++) {
				builder.AppendFormat("----- Exception {0} of {1} -----" + Environment.NewLine, i + 1, Exceptions.Count);
				builder.AppendLine(Exceptions[i].ToString());
			}
			return builder.ToString();
		}
	}
}
