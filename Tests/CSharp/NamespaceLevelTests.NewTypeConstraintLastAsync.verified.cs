
public partial interface Foo
{
}

public partial class Bar<x> where x : Foo, new()
{

}