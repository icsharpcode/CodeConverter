using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.CompilerServices;

namespace VbLibrary
{
    static class Module1
    {
        private static Dictionary<int, int> dict = new Dictionary<int, int>();

        private static void UseOutParameterInModule()
        {
            var x = default(object);
            int argvalue = Conversions.ToInteger(x);
            dict.TryGetValue(1, out argvalue);
        }

        public static void Main()
        {
            Console.Write((int)AClass.NestedEnum.First);
        }
    }
}