
using System;

namespace Issue1154
{
    [CaseSensitive1.Casesensitive1.TestDummy]
    public partial class UpperLowerCase
    {
    }

    [Casesensitive2.CaseSensitive2.TestDummy]
    public partial class LowerUpperCase
    {
    }

    [CaseSensitive3.CaseSensitive3.TestDummy]
    public partial class SameCase
    {
    }
}

namespace CaseSensitive1
{
    public partial class Casesensitive1
    {
        public partial class TestDummyAttribute : Attribute
        {
        }
    }
}

namespace Casesensitive2
{
    public partial class CaseSensitive2
    {
        public partial class TestDummyAttribute : Attribute
        {
        }
    }
}

namespace CaseSensitive3
{
    public partial class CaseSensitive3
    {
        public partial class TestDummyAttribute : Attribute
        {
        }
    }
}
