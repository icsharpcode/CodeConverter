
namespace VisualBasicUsesCSharpRefReturn
{
    public class WithRefReturnStructure
    {
        public void UseArr()
        {
            var arr = default(SomeStruct[]);
            var s = default(string);

            {
                ref var withBlock = ref arr[0];
                withBlock.P = s;
                s = withBlock.P;
            }
        }

        public void UseRefReturn()
        {
            var lst = default(CSharpRefReturn.RefReturnList<SomeStruct>);
            var s = default(string);

            {
                ref var withBlock = ref lst[0];
                withBlock.P = s;
                s = withBlock.P;
            }

            {
                ref var withBlock1 = ref lst.RefProperty;
                withBlock1.P = s;
                s = withBlock1.P;
            }
        }

        public struct SomeStruct
        {
            public string P { get; set; }
        }
    }
}