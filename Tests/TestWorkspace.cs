using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace RefactoringEssentials.Tests
{
	internal class TestWorkspace : Workspace
	{
		readonly static HostServices services = Microsoft.CodeAnalysis.Host.Mef.MefHostServices.DefaultHost;/* MefHostServices.Create(new [] {
				typeof(MefHostServices).Assembly,
				typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions).Assembly
			});*/


		public TestWorkspace(string workspaceKind = "Test") : base(services, workspaceKind)
		{
			/*
			foreach (var a in MefHostServices.DefaultAssemblies)
			{
				Console.WriteLine(a.FullName);
			}*/
		}

		public void ChangeDocument(DocumentId id, SourceText text)
		{
			ApplyDocumentTextChanged(id, text);
		}

		protected override void ApplyDocumentTextChanged(DocumentId id, SourceText text)
		{
			base.ApplyDocumentTextChanged(id, text);
			var document = CurrentSolution.GetDocument(id);
			if (document != null)
				OnDocumentTextChanged(id, text, PreservationMode.PreserveValue);
		}

		public override bool CanApplyChange(ApplyChangesKind feature)
		{
			return true;
		}

		public void Open(ProjectInfo projectInfo)
		{
			var sInfo = SolutionInfo.Create(
				SolutionId.CreateNewId(),
				VersionStamp.Create(),
				null,
				new[] { projectInfo }
			);
			OnSolutionAdded(sInfo);
		}
	}
}
