using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.CompilerServices;

namespace VbLibrary
{
    internal partial class AClass
    {
        public AClass()
        {
            anIntWithNonStaticInitializerReferencingOtherPart = anArrayWithNonStaticInitializerReferencingOtherPart.Length;
            anArrayWithNonStaticInitializerReferencingOtherPart = initialAnArrayWithNonStaticInitializerReferencingOtherPart();
        }

        public enum NestedEnum
        {
            First
        }

        private Dictionary<int, int> dict = new Dictionary<int, int>();
        private int anInt = 2;
        private int anIntWithNonStaticInitializerReferencingOtherPart;

        private void UseOutParameterInClass()
        {
            var x = default(object);
            int argvalue = Conversions.ToInteger(x);
            dict.TryGetValue(1, out argvalue);
        }

        private void UseEnumFromOtherFileInSolution(AnEnumType m)
        {
            string nothing = Enumerable.Empty<string>().ToArray()[(int)AnEnumType.AnEnumMember];
            switch (m)
            {
                case (AnEnumType)(-1):
                    {
                        return;
                    }

                case AnEnumType.AnEnumMember:
                    {
                        return;
                    }
            }
        }
    }
}