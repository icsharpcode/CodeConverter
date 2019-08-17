using System.Diagnostics;

namespace WindowsAppVb
{
    namespace My
    {

        // NOTE: This file is auto-generated; do not modify it directly.  To make changes,
        // or if you encounter build errors in this file, go to the Project Designer
        // (go to Project Properties or double-click the My Project node in
        // Solution Explorer), and make changes on the Application tab.
        // 
        internal partial class MyApplication
        {
            [DebuggerStepThrough()]
            public MyApplication() : base(Microsoft.VisualBasic.ApplicationServices.AuthenticationMode.Windows)
            {
                this.IsSingleInstance = false;
                this.EnableVisualStyles = true;
                this.SaveMySettingsOnExit = true;
                this.ShutdownStyle = Microsoft.VisualBasic.ApplicationServices.ShutdownMode.AfterMainFormCloses;
            }

            [DebuggerStepThrough()]
            protected override void OnCreateMainForm()
            {
                this.MainForm = WindowsAppVb.WinformsDesignerTest;
            }
        }
    }
}
