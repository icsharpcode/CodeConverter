namespace WindowsAppVb
{
    public static class ReferencingFormThroughStatic
    {
        public static string GetFormTitle()
        {
            return My.MyProject.MyForms.WinformsDesignerTest.Text;
        }
    }
}
