
namespace VbNetStandardLib
{
    public class AnInterfaceImplementation : AnInterface

    {

        public string APropertyWithDifferentName
        {
            get
            {
                return "Const";
            }
        }

        string AnInterface.AnInterfaceProperty { get => APropertyWithDifferentName; }

        public void AMethodWithDifferentName()
        {
        }

        void AnInterface.AnInterfaceMethod() => AMethodWithDifferentName();
    }
}