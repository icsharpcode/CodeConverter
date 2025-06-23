using System;

public partial class X
{
    [Display(Name = "Reinsurance Year")]
    public short SelectedReinsuranceYear;


    [Display(Name = "Record Type")]
    public string SelectedRecordType;

    [Display(Name = "Release Date")]
    public DateTime? ReleaseDate;

}

internal partial class DisplayAttribute : Attribute
{
    public string Name { get; set; }
}