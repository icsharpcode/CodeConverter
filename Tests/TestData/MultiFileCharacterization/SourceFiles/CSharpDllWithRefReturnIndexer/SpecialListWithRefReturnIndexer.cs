using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpDllWithRefReturnIndexer
{
    public class SpecialListWithRefReturnIndexer<T>
    {
        private T dummy;
        public ref T this[int i] {
            get {
                return ref dummy;
            }
        }
    }
}
