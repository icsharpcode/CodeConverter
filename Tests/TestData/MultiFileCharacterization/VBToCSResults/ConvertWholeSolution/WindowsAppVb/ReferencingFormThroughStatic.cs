namespace WindowsAppVb
{
    public static class ReferencingFormThroughStatic
    {
        public static string GetFormTitle()
        {
            if (Global.WindowsAppVb.My.MyProject.Forms.m_WinformsDesignerTest != null && My.MyProject.Forms.WinformsDesignerTest.Text != null)
                return My.MyProject.Forms.WinformsDesignerTest.Text;
            return "";
        }
    }
}
