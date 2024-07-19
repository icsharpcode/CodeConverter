using Microsoft.VisualBasic.CompilerServices;

namespace VisualBasicUsesCSharpRefReturn
{
    public class ByRefArgument
    {
        public void UseArr()
        {
            var arrObj = default(object[]);
            Modify(ref arrObj[0]);

            var arrInt = default(int[]);
            var tmp = arrInt;
            object argo = tmp[0];
            Modify(ref argo);
            tmp[0] = Conversions.ToInteger(argo);
        }

        public void UseRefReturn()
        {
            var lstObj = default(CSharpRefReturn.RefReturnList<object>);
            Modify(ref lstObj[0]);
            Modify(ref lstObj.RefProperty);

            var lstInt = default(CSharpRefReturn.RefReturnList<int>);
            var tmp = lstInt;
            object argo = tmp[0];
            Modify(ref argo);
            tmp[0] = Conversions.ToInteger(argo);
            object argo1 = lstInt.RefProperty;
            Modify(ref argo1);
            lstInt.RefProperty = Conversions.ToInteger(argo1);
        }

        public void Modify(ref object o)
        {
        }
    }
}