
internal partial class SurroundingClass
{
    public object Prop
    {
        get
        {
            object PropRet = default;
            try
            {
                PropRet = new object();
                return PropRet;
            }
            catch (Exception ex)
            {
            }

            return PropRet;
        }
    }

    public object Func()
    {
        object FuncRet = default;
        try
        {
            FuncRet = new object();
            return FuncRet;
        }
        catch (Exception ex)
        {
        }

        return FuncRet;
    }
}