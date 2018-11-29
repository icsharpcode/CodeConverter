using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class SolutionAndProjectTests : ProjectConverterTestBase
    {
        [Fact]
        public void ConvertSolution()
        {
            ConvertProjectsWhere<VBToCSConversion>(p => true);
        }

        [Fact]
        public void ConvertSingleProject()
        {
            ConvertProjectsWhere<VBToCSConversion>(p => p.Name == "VisualBasicLibrary");
        }
    }
}
