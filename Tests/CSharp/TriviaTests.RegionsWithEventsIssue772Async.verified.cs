using System;

public partial class VisualBasicClass : System.Windows.Forms.Form
{

    #region  Members 

    private string _Member = string.Empty;

    #endregion

    #region  Construction 

    public VisualBasicClass()
    {
        Load += Eventhandler_Load;

    }

    #endregion

    #region  Methods 

    public void Eventhandler_Load(object sender, EventArgs e)
    {
        // Do something
    }

    #endregion

}