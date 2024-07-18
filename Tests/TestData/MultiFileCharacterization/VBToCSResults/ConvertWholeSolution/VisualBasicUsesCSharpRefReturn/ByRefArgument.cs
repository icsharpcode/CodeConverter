
namespace VisualBasicUsesCSharpRefReturn
{
    public class ByRefArgument
    {
        public void UseArr()
        {
            var arr = default(object[]);
            Modify(ref arr[0]);
        }

        public void UseRefReturn()
        {
            var lst = default(CSharpRefReturn.RefReturnList<object>);
            Modify(ref lst[0]);
        }

        public void Modify(ref object o)
        {
        }
    }
}