namespace WindowsAppVb
{
    public static class ReferencingFormThroughStatic
    {
        public static string GetFormTitle()
        {
            if (My.MyProject.Forms.m_WinformsDesignerTest != null && My.MyProject.MyForms.WinformsDesignerTest.Text != null)
                return My.MyProject.MyForms.WinformsDesignerTest.Text;
            return "";
        }
    }
}
