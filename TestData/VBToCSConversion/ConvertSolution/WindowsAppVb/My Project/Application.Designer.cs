

namespace WindowsAppVb.My
{

    // NOTE: This file is auto-generated; do not modify it directly.  To make changes,
    // or if you encounter build errors in this file, go to the Project Designer
    // (go to Project Properties or double-click the My Project node in
    // Solution Explorer), and make changes on the Application tab.
    // 
    internal partial class MyApplication
    {
        [global::System.Diagnostics.DebuggerStepThrough()]
        public MyApplication() : base(global::Microsoft.VisualBasic.ApplicationServices.AuthenticationMode.Windows)
        {
            this.IsSingleInstance = false;
            this.EnableVisualStyles = true;
            this.SaveMySettingsOnExit = true;
            this.ShutDownStyle = global::Microsoft.VisualBasic.ApplicationServices.ShutdownMode.AfterMainFormCloses;
        }

        [global::System.Diagnostics.DebuggerStepThrough()]
        protected override void OnCreateMainForm()
        {
            this.MainForm = global::WindowsAppVb.Form1;
        }
    }
}

