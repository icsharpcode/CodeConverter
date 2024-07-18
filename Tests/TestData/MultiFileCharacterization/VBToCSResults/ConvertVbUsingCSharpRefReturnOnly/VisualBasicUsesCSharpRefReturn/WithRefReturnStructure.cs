
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
            CSharpRefReturn.RefReturnList<SomeStruct> lst;
            string s;

            // With lst(0)
            // .P = s
            // s = .P
            // End With
        }

        public struct SomeStruct
        {
            public string P { get; set; }
        }
    }
}