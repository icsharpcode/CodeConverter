
public partial class Class1
{
    private Class1 C1 { get; set; }
    private Class1 _c2;
    private object _o1;

    public void Foo()
    {
        object argclass1 = new Class1();
        Bar(ref argclass1);
        object argclass11 = C1;
        Bar(ref argclass11);
        C1 = (Class1)argclass11;
        object argclass12 = C1;
        Bar(ref argclass12);
        C1 = (Class1)argclass12;
        object argclass13 = _c2;
        Bar(ref argclass13);
        _c2 = (Class1)argclass13;
        object argclass14 = _c2;
        Bar(ref argclass14);
        _c2 = (Class1)argclass14;
        Bar(ref _o1);
        Bar(ref _o1);
    }

    public void Bar(ref object class1)
    {
    }
}