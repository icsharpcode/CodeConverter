using System;
using System.Diagnostics;
using System.ComponentModel;

namespace WindowsAppVb.My
{
    internal static partial class MyProject
    {
        internal partial class MyForms
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            public WinformsDesignerTest m_WinformsDesignerTest;

            public WinformsDesignerTest WinformsDesignerTest
            {
                [DebuggerHidden]
                get
                {
                    m_WinformsDesignerTest = MyForms.Create__Instance__(m_WinformsDesignerTest);
                    return m_WinformsDesignerTest;
                }
                [DebuggerHidden]
                set
                {
                    if (value == m_WinformsDesignerTest)
                        return;
                    if (value != null)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_WinformsDesignerTest);
                }
            }
        }
    }
}
