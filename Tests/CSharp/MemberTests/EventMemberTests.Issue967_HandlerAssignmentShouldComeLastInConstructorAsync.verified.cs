using System.Windows.Forms;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class MainWindow : Form
{
    public MainWindow()
    {
        InitializeComponent();
        Load += (_, __) => MainWindow_Loaded();
    }

    private void MainWindow_Loaded()
    {
        Interaction.MsgBox("Window, loaded");
    }
}

public partial class MainWindow
{
    public void InitializeComponent()
    {
    }
}
