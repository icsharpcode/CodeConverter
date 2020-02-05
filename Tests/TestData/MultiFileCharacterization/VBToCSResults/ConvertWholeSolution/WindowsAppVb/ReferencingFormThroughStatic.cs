namespace WindowsAppVb
{
    public static class ReferencingFormThroughStatic
    {
        public static string GetFormTitle()
        {
            {
                var withBlock = new System.Text.StringBuilder();
                withBlock.Capacity = 4;
            }

            if (My.MyProject.Forms.m_WinformsDesignerTest != null && My.MyProject.Forms.WinformsDesignerTest.Text != null)
            {
                return My.MyProject.Forms.WinformsDesignerTest.Text;
            }

            return "";
        }
    }
}