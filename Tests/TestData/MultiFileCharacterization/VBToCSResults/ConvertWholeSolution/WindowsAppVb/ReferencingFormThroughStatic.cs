
namespace WindowsAppVb
{
    public static class ReferencingFormThroughStatic
    {
        public static string GetFormTitle()
        {
            // This used to cause a bug in the expander for maes.Expression leading to another bug later on
            {
                var withBlock = new System.Text.StringBuilder();
                withBlock.Capacity = 4;
            }

            if (My.MyProject.Forms.m_WinformsDesignerTest is not null && My.MyProject.Forms.WinformsDesignerTest.Text is not null)
            {
                return My.MyProject.Forms.WinformsDesignerTest.Text;
            }
            return "";
        }
    }
}