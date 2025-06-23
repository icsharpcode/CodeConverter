using System;

namespace InnerNamespace
{
    public partial class Test
    {
        public string InterStringDateFormat(DateTime dt)
        {
            string a = $"Soak: {dt: d\\.h\\:mm\\:ss\\.f}";
            return a;
        }
    }
}