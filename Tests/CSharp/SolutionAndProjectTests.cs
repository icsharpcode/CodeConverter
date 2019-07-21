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
    [Collection(MsBuildFixture.Collection)]
    public class SolutionAndProjectTests
    {
        private readonly MsBuildFixture _msBuildFixture;

        public SolutionAndProjectTests(MsBuildFixture msBuildFixture)
        {
            _msBuildFixture = msBuildFixture;
        }

        [Fact]
        public async Task ConvertSolution()
        {
            await _msBuildFixture.ConvertProjectsWhere<VBToCSConversion>(p => true);
        }

        [Fact]
        public async Task ConvertSingleProject()
        {
            await _msBuildFixture.ConvertProjectsWhere<VBToCSConversion>(p => p.Name == "EmptyVb");
        }
    }
}
