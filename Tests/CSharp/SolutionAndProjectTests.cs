using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.CSharp;
using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class SolutionAndProjectTests : ProjectConverterTestBase
    {
        [Fact]
        public async Task ConvertSolution()
        {
            await ConvertProjectsWhere<VBToCSConversion>(p => true);
        }

        [Fact]
        public async Task ConvertSingleProject()
        {
            await ConvertProjectsWhere<VBToCSConversion>(p => p.Name == "VisualBasicLibrary");
        }
    }
}
