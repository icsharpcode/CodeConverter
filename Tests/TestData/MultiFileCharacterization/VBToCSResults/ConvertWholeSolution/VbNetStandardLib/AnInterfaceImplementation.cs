
namespace VbNetStandardLib
{
    public class AnInterfaceImplementation : AnInterface
    {
        public string AnInterfaceProperty
        {
            get
            {
                return "Const";
            }
        }

        public void AnInterfaceMethod()
        {
        }

        public void AMethodWithDifferentName() => AnInterfaceMethod();
    }
}