
public partial class Class1
{
    public void Foo()
    {
        for (this.Index = 0; this.Index <= 10; this.Index++)
        {

        }
    }
}
1 source compilation errors:
BC30456: 'Index' is not a member of 'Class1'.
1 target compilation errors:
CS1061: 'Class1' does not contain a definition for 'Index' and no accessible extension method 'Index' accepting a first argument of type 'Class1' could be found (are you missing a using directive or an assembly reference?)