using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace EmptyVb
{
    internal class AClass
    {
        private void UseEnumFromOtherFileInSolution(AnEnum m)
        {
            var nothing = Enumerable.Empty<string>().ToArray()[(int)AnEnum.AnEnumMember];
            switch (m)
            {
                case -1:
                    {
                        return;
                    }

                case AnEnum.AnEnumMember:
                    {
                        return;
                    }
            }
        }
    }
}
