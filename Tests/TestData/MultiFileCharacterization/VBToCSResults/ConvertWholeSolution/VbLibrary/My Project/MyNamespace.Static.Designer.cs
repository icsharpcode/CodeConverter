// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualBasic.CompilerServices;

namespace Microsoft.VisualBasic
{
    [Embedded()]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Module | AttributeTargets.Assembly, Inherited = false)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.CompilerServices.CompilerGenerated()]
    internal sealed class Embedded : Attribute
    {
    }
}

namespace VbLibrary
{

    /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
    /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia */
    /* TODO ERROR: Skipped DefineDirectiveTrivia *//* TODO ERROR: Skipped DefineDirectiveTrivia *//* TODO ERROR: Skipped DefineDirectiveTrivia *//* TODO ERROR: Skipped DefineDirectiveTrivia */
    /* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElifDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
    /* TODO ERROR: Skipped IfDirectiveTrivia */
    namespace My
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
                    if (instance == null)
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
                    instance = default(T);
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
                        if (m_ThreadStaticValue == null)
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
    // Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

    // See Compiler::LoadXmlSolutionExtension
    namespace My
    {
        [Embedded()]
        [DebuggerNonUserCode()]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal sealed class InternalXmlHelper
        {
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            private InternalXmlHelper()
            {
            }

            public static string get_Value(IEnumerable<XElement> source)
            {
                foreach (XElement item in source)
                    return item.Value;
                return null;
            }

            public static void set_Value(IEnumerable<XElement> source, string value)
            {
                foreach (XElement item in source)
                {
                    item.Value = value;
                    break;
                }
            }

            public static string get_AttributeValue(IEnumerable<XElement> source, XName name)
            {
                foreach (XElement item in source)
                    return Conversions.ToString(item.Attribute(name));
                return null;
            }

            public static void set_AttributeValue(IEnumerable<XElement> source, XName name, string value)
            {
                foreach (XElement item in source)
                {
                    item.SetAttributeValue(name, value);
                    break;
                }
            }

            public static string get_AttributeValue(XElement source, XName name)
            {
                return Conversions.ToString(source.Attribute(name));
            }

            public static void set_AttributeValue(XElement source, XName name, string value)
            {
                source.SetAttributeValue(name, value);
            }

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static XAttribute CreateAttribute(XName name, object value)
            {
                if (value == null)
                {
                    return null;
                }

                return new XAttribute(name, value);
            }

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static XAttribute CreateNamespaceAttribute(XName name, XNamespace ns)
            {
                var a = new XAttribute(name, ns.NamespaceName);
                a.AddAnnotation(ns);
                return a;
            }

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static object RemoveNamespaceAttributes(string[] inScopePrefixes, XNamespace[] inScopeNs, List<XAttribute> attributes, object obj)
            {
                if (obj != null)
                {
                    XElement elem = obj as XElement;
                    if (!(elem == null))
                    {
                        return RemoveNamespaceAttributes(inScopePrefixes, inScopeNs, attributes, elem);
                    }
                    else
                    {
                        IEnumerable elems = obj as IEnumerable;
                        if (elems != null)
                        {
                            return RemoveNamespaceAttributes(inScopePrefixes, inScopeNs, attributes, elems);
                        }
                    }
                }

                return obj;
            }

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static IEnumerable RemoveNamespaceAttributes(string[] inScopePrefixes, XNamespace[] inScopeNs, List<XAttribute> attributes, IEnumerable obj)
            {
                if (obj != null)
                {
                    IEnumerable<XElement> elems = obj as IEnumerable<XElement>;
                    if (elems != null)
                    {
                        return elems.Select(new RemoveNamespaceAttributesClosure(inScopePrefixes, inScopeNs, attributes).ProcessXElement);
                    }
                    else
                    {
                        return obj.Cast<object>().Select(new RemoveNamespaceAttributesClosure(inScopePrefixes, inScopeNs, attributes).ProcessObject);
                    }
                }

                return obj;
            }

            [DebuggerNonUserCode()]
            [System.Runtime.CompilerServices.CompilerGenerated()]
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            private sealed class RemoveNamespaceAttributesClosure
            {
                private readonly string[] m_inScopePrefixes;
                private readonly XNamespace[] m_inScopeNs;
                private readonly List<XAttribute> m_attributes;

                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                internal RemoveNamespaceAttributesClosure(string[] inScopePrefixes, XNamespace[] inScopeNs, List<XAttribute> attributes)
                {
                    m_inScopePrefixes = inScopePrefixes;
                    m_inScopeNs = inScopeNs;
                    m_attributes = attributes;
                }

                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                internal XElement ProcessXElement(XElement elem)
                {
                    return RemoveNamespaceAttributes(m_inScopePrefixes, m_inScopeNs, m_attributes, elem);
                }

                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                internal object ProcessObject(object obj)
                {
                    XElement elem = obj as XElement;
                    if (elem != null)
                    {
                        return RemoveNamespaceAttributes(m_inScopePrefixes, m_inScopeNs, m_attributes, elem);
                    }
                    else
                    {
                        return obj;
                    }
                }
            }

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static XElement RemoveNamespaceAttributes(string[] inScopePrefixes, XNamespace[] inScopeNs, List<XAttribute> attributes, XElement e)
            {
                if (e != null)
                {
                    var a = e.FirstAttribute;
                    while (a != null)
                    {
                        var nextA = a.NextAttribute;
                        if (a.IsNamespaceDeclaration)
                        {
                            var ns = a.Annotation<XNamespace>();
                            string prefix = a.Name.LocalName;
                            if (ns != null)
                            {
                                if (inScopePrefixes != null && inScopeNs != null)
                                {
                                    int lastIndex = inScopePrefixes.Length - 1;
                                    for (int i = 0, loopTo = lastIndex; i <= loopTo; i++)
                                    {
                                        string currentInScopePrefix = inScopePrefixes[i];
                                        var currentInScopeNs = inScopeNs[i];
                                        if (prefix.Equals(currentInScopePrefix))
                                        {
                                            if (ns == currentInScopeNs)
                                            {
                                                // prefix and namespace match.  Remove the unneeded ns attribute 
                                                a.Remove();
                                            }

                                            // prefix is in scope but refers to something else.  Leave the ns attribute. 
                                            a = null;
                                            break;
                                        }
                                    }
                                }

                                if (a != null)
                                {
                                    // Prefix is not in scope 
                                    // Now check whether it's going to be in scope because it is in the attributes list 

                                    if (attributes != null)
                                    {
                                        int lastIndex = attributes.Count - 1;
                                        for (int i = 0, loopTo1 = lastIndex; i <= loopTo1; i++)
                                        {
                                            var currentA = attributes[i];
                                            string currentInScopePrefix = currentA.Name.LocalName;
                                            var currentInScopeNs = currentA.Annotation<XNamespace>();
                                            if (currentInScopeNs != null)
                                            {
                                                if (prefix.Equals(currentInScopePrefix))
                                                {
                                                    if (ns == currentInScopeNs)
                                                    {
                                                        // prefix and namespace match.  Remove the unneeded ns attribute 
                                                        a.Remove();
                                                    }

                                                    // prefix is in scope but refers to something else.  Leave the ns attribute. 
                                                    a = null;
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (a != null)
                                    {
                                        // Prefix is definitely not in scope  
                                        a.Remove();
                                        // namespace is not defined either.  Add this attributes list 
                                        attributes.Add(a);
                                    }
                                }
                            }
                        }

                        a = nextA;
                    }
                }

                return e;
            }
        }

        // Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

    }
}