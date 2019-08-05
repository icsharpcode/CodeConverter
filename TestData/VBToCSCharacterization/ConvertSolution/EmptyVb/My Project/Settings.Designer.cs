namespace EmptyVb
{
    namespace My
    {
        [System.Runtime.CompilerServices.CompilerGenerated()]
        [System.CodeDom.Compiler.GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        internal sealed partial class MySettings : System.Configuration.ApplicationSettingsBase
        {
            private static MySettings defaultInstance = (MySettings)Synchronized(new MySettings());

            /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            public static MySettings Default
            {
                get
                {

                    /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                    return defaultInstance;
                }
            }
        }
    }

    namespace My
    {
        [Microsoft.VisualBasic.HideModuleName()]
        [System.Diagnostics.DebuggerNonUserCode()]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        internal static class MySettingsProperty
        {
            [System.ComponentModel.Design.HelpKeyword("My.Settings")]
            internal static MySettings Settings
            {
                get
                {
                    return MySettings.Default;
                }
            }
        }
    }
}
