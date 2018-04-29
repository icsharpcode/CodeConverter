using System;
using System.IO;

namespace ICSharpCode.CodeConverter.Shared
{
    internal class PathConverter
    {
        public static string TogglePathExtension(string filePath)
        {
            var originalExtension = Path.GetExtension(filePath);
            return Path.ChangeExtension(filePath, GetConvertedExtension(originalExtension));
        }

        private static string GetConvertedExtension(string originalExtension)
        {
            switch (originalExtension)
            {
                case ".csproj":
                    return ".vbproj";
                case ".vbproj":
                    return ".csproj";
                case ".cs":
                    return ".vb";
                case ".vb":
                    return ".cs";
                case ".txt":
                    return ".txt";
                default:
                    throw new ArgumentOutOfRangeException(nameof(originalExtension), originalExtension, null);
            }
        }
    }
}