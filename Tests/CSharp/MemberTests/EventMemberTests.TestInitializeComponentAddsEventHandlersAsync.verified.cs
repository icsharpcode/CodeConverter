using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

[DesignerGenerated]
public partial class TestHandlesAdded
{
    public TestHandlesAdded()
    {
        InitializeComponent();
    }

    public void InitializeComponent()
    {
        POW_btnV2DBM = new System.Windows.Forms.Button();
        POW_btnV2DBM.Click += POW_btnV2DBM_Click;
        // 
        // POW_btnV2DBM
        // 
        POW_btnV2DBM.Location = new System.Drawing.Point(207, 15);
        POW_btnV2DBM.Name = "POW_btnV2DBM";
        POW_btnV2DBM.Size = new System.Drawing.Size(42, 23);
        POW_btnV2DBM.TabIndex = 3;
        POW_btnV2DBM.Text = ">>";
        POW_btnV2DBM.UseVisualStyleBackColor = true;
    }

}

public partial class TestHandlesAdded
{
    private Button POW_btnV2DBM;

    public void POW_btnV2DBM_Click()
    {

    }
}
2 source compilation errors:
BC30002: Type 'Button' is not defined.
BC30590: Event 'Click' cannot be found.
1 target compilation errors:
CS0246: The type or namespace name 'Button' could not be found (are you missing a using directive or an assembly reference?)