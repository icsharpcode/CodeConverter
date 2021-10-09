using System.Collections.Generic;
using static System.Linq.Enumerable;
using static VbNetStandardLib.OuterClass;

namespace VbLibrary.ClassImport
{
    class ClassImportClass
    {
        public IEnumerable<string> ClassImportUnQualifiedMember()
        {
            return Empty<string>();
        }

        public void ClassImportUnQualifiedNestedType()
        {
            var ic = new InnerClass();
            ic.TestMethod();
        }
    }
}