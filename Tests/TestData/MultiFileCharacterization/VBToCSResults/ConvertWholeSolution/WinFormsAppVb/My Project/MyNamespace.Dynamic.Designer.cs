using System;
using System.ComponentModel;
using System.Diagnostics;

namespace WinFormsAppVb.My
{
    internal static partial class MyProject
    {
        internal partial class MyForms
        {

            [EditorBrowsable(EditorBrowsableState.Never)]
            public FolderForm m_FolderForm;

            public FolderForm FolderForm
            {
                [DebuggerHidden]
                get
                {
                    m_FolderForm = Create__Instance__(m_FolderForm);
                    return m_FolderForm;
                }
                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_FolderForm))
                        return;
                    if (value is not null)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_FolderForm);
                }
            }


            [EditorBrowsable(EditorBrowsableState.Never)]
            public Form1 m_Form1;

            public Form1 Form1
            {
                [DebuggerHidden]
                get
                {
                    m_Form1 = Create__Instance__(m_Form1);
                    return m_Form1;
                }
                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_Form1))
                        return;
                    if (value is not null)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_Form1);
                }
            }


            [EditorBrowsable(EditorBrowsableState.Never)]
            public WinformsDesignerTest m_WinformsDesignerTest;

            public WinformsDesignerTest WinformsDesignerTest
            {
                [DebuggerHidden]
                get
                {
                    m_WinformsDesignerTest = Create__Instance__(m_WinformsDesignerTest);
                    return m_WinformsDesignerTest;
                }
                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_WinformsDesignerTest))
                        return;
                    if (value is not null)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_WinformsDesignerTest);
                }
            }

        }


    }
}