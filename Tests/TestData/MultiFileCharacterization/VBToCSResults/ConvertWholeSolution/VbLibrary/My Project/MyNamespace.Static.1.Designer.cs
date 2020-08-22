// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.VisualBasic;

/* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
/* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia */
/* TODO ERROR: Skipped DefineDirectiveTrivia *//* TODO ERROR: Skipped DefineDirectiveTrivia *//* TODO ERROR: Skipped DefineDirectiveTrivia *//* TODO ERROR: Skipped DefineDirectiveTrivia */
/* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
/* TODO ERROR: Skipped IfDirectiveTrivia */
namespace VbLibrary.My
{

    /* TODO ERROR: Skipped IfDirectiveTrivia */
    [System.CodeDom.Compiler.GeneratedCode("MyTemplate", "11.0.0.0")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]

    /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia */
    internal partial class MyApplication : Microsoft.VisualBasic.ApplicationServices.ConsoleApplicationBase
    {
        /* TODO ERROR: Skipped EndIfDirectiveTrivia */
    }

    /* TODO ERROR: Skipped EndIfDirectiveTrivia */
    /* TODO ERROR: Skipped IfDirectiveTrivia */
    [System.CodeDom.Compiler.GeneratedCode("MyTemplate", "11.0.0.0")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]

    /* TODO ERROR: Skipped IfDirectiveTrivia */
    internal partial class MyComputer : Microsoft.VisualBasic.Devices.Computer
    {
        /* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
        [DebuggerHidden()]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MyComputer() : base()
        {
        }
    }
    /* TODO ERROR: Skipped EndIfDirectiveTrivia */
    [HideModuleName()]
    [System.CodeDom.Compiler.GeneratedCode("MyTemplate", "11.0.0.0")]
    internal static class MyProject
    {

        /* TODO ERROR: Skipped IfDirectiveTrivia */
        [System.ComponentModel.Design.HelpKeyword("My.Computer")]
        internal static MyComputer Computer
        {
            [DebuggerHidden()]
            get
            {
                return m_ComputerObjectProvider.GetInstance;
            }
        }

        private readonly static ThreadSafeObjectProvider<MyComputer> m_ComputerObjectProvider = new ThreadSafeObjectProvider<MyComputer>();
        /* TODO ERROR: Skipped EndIfDirectiveTrivia */
        /* TODO ERROR: Skipped IfDirectiveTrivia */
        [System.ComponentModel.Design.HelpKeyword("My.Application")]
        internal static MyApplication Application
        {
            [DebuggerHidden()]
            get
            {
                return m_AppObjectProvider.GetInstance;
            }
        }

        private readonly static ThreadSafeObjectProvider<MyApplication> m_AppObjectProvider = new ThreadSafeObjectProvider<MyApplication>();
        /* TODO ERROR: Skipped EndIfDirectiveTrivia */
        /* TODO ERROR: Skipped IfDirectiveTrivia */
        [System.ComponentModel.Design.HelpKeyword("My.User")]
        internal static Microsoft.VisualBasic.ApplicationServices.User User
        {
            [DebuggerHidden()]
            get
            {
                return m_UserObjectProvider.GetInstance;
            }
        }

        private readonly static ThreadSafeObjectProvider<Microsoft.VisualBasic.ApplicationServices.User> m_UserObjectProvider = new ThreadSafeObjectProvider<Microsoft.VisualBasic.ApplicationServices.User>();
        /* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
        /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
        /* TODO ERROR: Skipped IfDirectiveTrivia */
        [System.ComponentModel.Design.HelpKeyword("My.WebServices")]
        internal static MyWebServices WebServices
        {
            [DebuggerHidden()]
            get
            {
                return m_MyWebServicesObjectProvider.GetInstance;
            }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [MyGroupCollection("System.Web.Services.Protocols.SoapHttpClientProtocol", "Create__Instance__", "Dispose__Instance__", "")]
        internal sealed class MyWebServices
        {
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [DebuggerHidden()]
            public override bool Equals(object o)
            {
                return base.Equals(o);
            }

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [DebuggerHidden()]
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [DebuggerHidden()]
            internal new Type GetType()
            {
                return typeof(MyWebServices);
            }

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [DebuggerHidden()]
            public override string ToString()
            {
                return base.ToString();
            }

            [DebuggerHidden()]
            private static T Create__Instance__<T>(T instance) where T : new()
            {
                if (instance is null)
                {
                    return new T();
                }
                else
                {
                    return instance;
                }
            }

            [DebuggerHidden()]
            private void Dispose__Instance__<T>(ref T instance)
            {
                instance = default;
            }

            [DebuggerHidden()]
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public MyWebServices() : base()
            {
            }
        }

        private readonly static ThreadSafeObjectProvider<MyWebServices> m_MyWebServicesObjectProvider = new ThreadSafeObjectProvider<MyWebServices>();
        /* TODO ERROR: Skipped EndIfDirectiveTrivia */
        /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Runtime.InteropServices.ComVisible(false)]
        internal sealed class ThreadSafeObjectProvider<T> where T : new()
        {
            internal T GetInstance
            {
                /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElseDirectiveTrivia */
                [DebuggerHidden()]
                get
                {
                    if (m_ThreadStaticValue is null)
                        m_ThreadStaticValue = new T();
                    return m_ThreadStaticValue;
                }
                /* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }

            [DebuggerHidden()]
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public ThreadSafeObjectProvider() : base()
            {
            }

            /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElseDirectiveTrivia */
            [System.Runtime.CompilerServices.CompilerGenerated()]
            [ThreadStatic()]
            private static T m_ThreadStaticValue;
            /* TODO ERROR: Skipped EndIfDirectiveTrivia */
        }
    }
}
/* TODO ERROR: Skipped EndIfDirectiveTrivia */
