
public partial class BaseController
{
    protected HttpRequest Request;
}

public partial class ActualController : BaseController
{

    public void Do()
    {
        Request.StatusCode = 200;
    }
}
2 source compilation errors:
BC30183: Keyword is not valid as an identifier.
BC30002: Type 'HttpRequest' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'HttpRequest' could not be found (are you missing a using directive or an assembly reference?)