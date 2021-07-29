
namespace VbNetStandardLib
{
    public class AnInterfaceImplementation : AnInterface
    {
        string AnInterface.AnInterfaceProperty
        {
            get
            {
                return "Const";
            }
        }

        public string APropertyWithDifferentName
        {
            get => ((AnInterface)this).AnInterfaceProperty;
        }

        void AnInterface.AnInterfaceMethod()
        {
        }

        public void AMethodWithDifferentName() => ((AnInterface)this).AnInterfaceMethod();
    }
}