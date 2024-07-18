// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.VisualBasic;

/* TODO ERROR: Skipped IfDirectiveTrivia
#If TARGET = "module" AndAlso _MYTYPE = "" Then
*//* TODO ERROR: Skipped DisabledTextTrivia
#Const _MYTYPE="Empty"
*//* TODO ERROR: Skipped EndIfDirectiveTrivia
#End If
*/
/* TODO ERROR: Skipped IfDirectiveTrivia
#If _MYTYPE = "WindowsForms" Then
*//* TODO ERROR: Skipped DisabledTextTrivia

#Const _MYFORMS = True
#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Windows"
#Const _MYCOMPUTERTYPE = "Windows"
#Const _MYAPPLICATIONTYPE = "WindowsForms"

*//* TODO ERROR: Skipped ElifDirectiveTrivia
#ElseIf _MYTYPE = "WindowsFormsWithCustomSubMain" Then
*//* TODO ERROR: Skipped DisabledTextTrivia

#Const _MYFORMS = True
#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Windows"
#Const _MYCOMPUTERTYPE = "Windows"
#Const _MYAPPLICATIONTYPE = "Console"

*//* TODO ERROR: Skipped ElifDirectiveTrivia
#ElseIf _MYTYPE = "Windows" OrElse _MYTYPE = "" Then
*/
/* TODO ERROR: Skipped DefineDirectiveTrivia
#Const _MYWEBSERVICES = True
*//* TODO ERROR: Skipped DefineDirectiveTrivia
#Const _MYUSERTYPE = "Windows"
*//* TODO ERROR: Skipped DefineDirectiveTrivia
#Const _MYCOMPUTERTYPE = "Windows"
*//* TODO ERROR: Skipped DefineDirectiveTrivia
#Const _MYAPPLICATIONTYPE = "Windows"
*/
/* TODO ERROR: Skipped ElifDirectiveTrivia
#ElseIf _MYTYPE = "Console" Then
*//* TODO ERROR: Skipped DisabledTextTrivia

#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Windows"
#Const _MYCOMPUTERTYPE = "Windows"
#Const _MYAPPLICATIONTYPE = "Console"

*//* TODO ERROR: Skipped ElifDirectiveTrivia
#ElseIf _MYTYPE = "Web" Then
*//* TODO ERROR: Skipped DisabledTextTrivia

#Const _MYFORMS = False
#Const _MYWEBSERVICES = False
#Const _MYUSERTYPE = "Web"
#Const _MYCOMPUTERTYPE = "Web"

*//* TODO ERROR: Skipped ElifDirectiveTrivia
#ElseIf _MYTYPE = "WebControl" Then
*//* TODO ERROR: Skipped DisabledTextTrivia

#Const _MYFORMS = False
#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Web"
#Const _MYCOMPUTERTYPE = "Web"

*//* TODO ERROR: Skipped ElifDirectiveTrivia
#ElseIf _MYTYPE = "Custom" Then
*//* TODO ERROR: Skipped DisabledTextTrivia

*//* TODO ERROR: Skipped ElifDirectiveTrivia
#ElseIf _MYTYPE <> "Empty" Then
*//* TODO ERROR: Skipped DisabledTextTrivia

#Const _MYTYPE = "Empty"

*//* TODO ERROR: Skipped EndIfDirectiveTrivia
#End If
*/
/* TODO ERROR: Skipped IfDirectiveTrivia
#If _MYTYPE <> "Empty" Then
*/
namespace VisualBasicUsesCSharpRefReturn.My
{

    /* TODO ERROR: Skipped IfDirectiveTrivia
    #If _MYAPPLICATIONTYPE = "WindowsForms" OrElse _MYAPPLICATIONTYPE = "Windows" OrElse _MYAPPLICATIONTYPE = "Console" Then
    */
    [System.CodeDom.Compiler.GeneratedCode("MyTemplate", "11.0.0.0")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]

    /* TODO ERROR: Skipped IfDirectiveTrivia
    #If _MYAPPLICATIONTYPE = "WindowsForms" Then
    *//* TODO ERROR: Skipped DisabledTextTrivia
            Inherits Global.Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
    #If TARGET = "winexe" Then
            <Global.System.STAThread(), Global.System.Diagnostics.DebuggerHidden(), Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Advanced)> _
            Friend Shared Sub Main(ByVal Args As String())
                Try
                   Global.System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(MyApplication.UseCompatibleTextRendering())
                Finally
                End Try               
                My.Application.Run(Args)
            End Sub
    #End If

    *//* TODO ERROR: Skipped ElifDirectiveTrivia
    #ElseIf _MYAPPLICATIONTYPE = "Windows" Then
    */
    internal partial class MyApplication : Microsoft.VisualBasic.ApplicationServices.ApplicationBase
    {
        /* TODO ERROR: Skipped ElifDirectiveTrivia
        #ElseIf _MYAPPLICATIONTYPE = "Console" Then
        *//* TODO ERROR: Skipped DisabledTextTrivia
                Inherits Global.Microsoft.VisualBasic.ApplicationServices.ConsoleApplicationBase	
        *//* TODO ERROR: Skipped EndIfDirectiveTrivia
        #End If '_MYAPPLICATIONTYPE = "WindowsForms"
        */
    }

    /* TODO ERROR: Skipped EndIfDirectiveTrivia
    #End If '#If _MYAPPLICATIONTYPE = "WindowsForms" Or _MYAPPLICATIONTYPE = "Windows" or _MYAPPLICATIONTYPE = "Console"
    */
    /* TODO ERROR: Skipped IfDirectiveTrivia
    #If _MYCOMPUTERTYPE <> "" Then
    */
    [System.CodeDom.Compiler.GeneratedCode("MyTemplate", "11.0.0.0")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]

    /* TODO ERROR: Skipped IfDirectiveTrivia
    #If _MYCOMPUTERTYPE = "Windows" Then
    */
    internal partial class MyComputer : Microsoft.VisualBasic.Devices.Computer
    {
        /* TODO ERROR: Skipped ElifDirectiveTrivia
        #ElseIf _MYCOMPUTERTYPE = "Web" Then
        *//* TODO ERROR: Skipped DisabledTextTrivia
                Inherits Global.Microsoft.VisualBasic.Devices.ServerComputer
        *//* TODO ERROR: Skipped EndIfDirectiveTrivia
        #End If
        */
        [DebuggerHidden()]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MyComputer() : base()
        {
        }
    }
    /* TODO ERROR: Skipped EndIfDirectiveTrivia
    #End If
    */
    [HideModuleName()]
    [System.CodeDom.Compiler.GeneratedCode("MyTemplate", "11.0.0.0")]
    internal static class MyProject
    {

        /* TODO ERROR: Skipped IfDirectiveTrivia
        #If _MYCOMPUTERTYPE <> "" Then
        */
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
        /* TODO ERROR: Skipped EndIfDirectiveTrivia
        #End If
        */
        /* TODO ERROR: Skipped IfDirectiveTrivia
        #If _MYAPPLICATIONTYPE = "Windows" Or _MYAPPLICATIONTYPE = "WindowsForms" Or _MYAPPLICATIONTYPE = "Console" Then
        */
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
        /* TODO ERROR: Skipped EndIfDirectiveTrivia
        #End If
        */
        /* TODO ERROR: Skipped IfDirectiveTrivia
        #If _MYUSERTYPE = "Windows" Then
        */
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
        /* TODO ERROR: Skipped ElifDirectiveTrivia
        #ElseIf _MYUSERTYPE = "Web" Then
        *//* TODO ERROR: Skipped DisabledTextTrivia
                <Global.System.ComponentModel.Design.HelpKeyword("My.User")> _
                Friend ReadOnly Property User() As Global.Microsoft.VisualBasic.ApplicationServices.WebUser
                    <Global.System.Diagnostics.DebuggerHidden()> _
                    Get
                        Return m_UserObjectProvider.GetInstance()
                    End Get
                End Property
                Private ReadOnly m_UserObjectProvider As New ThreadSafeObjectProvider(Of Global.Microsoft.VisualBasic.ApplicationServices.WebUser)
        *//* TODO ERROR: Skipped EndIfDirectiveTrivia
        #End If
        */
        /* TODO ERROR: Skipped IfDirectiveTrivia
        #If _MYFORMS = True Then
        *//* TODO ERROR: Skipped DisabledTextTrivia

        #Const STARTUP_MY_FORM_FACTORY = "My.MyProject.Forms"

                <Global.System.ComponentModel.Design.HelpKeyword("My.Forms")> _
                Friend ReadOnly Property Forms() As MyForms
                    <Global.System.Diagnostics.DebuggerHidden()> _
                    Get
                        Return m_MyFormsObjectProvider.GetInstance()
                    End Get
                End Property

                <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
                <Global.Microsoft.VisualBasic.MyGroupCollection("System.Windows.Forms.Form", "Create__Instance__", "Dispose__Instance__", "My.MyProject.Forms")> _
                Friend NotInheritable Class MyForms
                    <Global.System.Diagnostics.DebuggerHidden()> _
                    Private Shared Function Create__Instance__(Of T As {New, Global.System.Windows.Forms.Form})(ByVal Instance As T) As T
                        If Instance Is Nothing OrElse Instance.IsDisposed Then
                            If m_FormBeingCreated IsNot Nothing Then
                                If m_FormBeingCreated.ContainsKey(GetType(T)) = True Then
                                    Throw New Global.System.InvalidOperationException(Global.Microsoft.VisualBasic.CompilerServices.Utils.GetResourceString("WinForms_RecursiveFormCreate"))
                                End If
                            Else
                                m_FormBeingCreated = New Global.System.Collections.Hashtable()
                            End If
                            m_FormBeingCreated.Add(GetType(T), Nothing)
                            Try
                                Return New T()
                            Catch ex As Global.System.Reflection.TargetInvocationException When ex.InnerException IsNot Nothing
                                Dim BetterMessage As String = Global.Microsoft.VisualBasic.CompilerServices.Utils.GetResourceString("WinForms_SeeInnerException", ex.InnerException.Message)
                                Throw New Global.System.InvalidOperationException(BetterMessage, ex.InnerException)
                            Finally
                                m_FormBeingCreated.Remove(GetType(T))
                            End Try
                        Else
                            Return Instance
                        End If
                    End Function

                    <Global.System.Diagnostics.DebuggerHidden()> _
                    Private Sub Dispose__Instance__(Of T As Global.System.Windows.Forms.Form)(ByRef instance As T)
                        instance.Dispose()
                        instance = Nothing
                    End Sub

                    <Global.System.Diagnostics.DebuggerHidden()> _
                    <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
                    Public Sub New()
                       MyBase.New()
                    End Sub

                    <Global.System.ThreadStatic()> Private Shared m_FormBeingCreated As Global.System.Collections.Hashtable

                    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> Public Overrides Function Equals(ByVal o As Object) As Boolean
                        Return MyBase.Equals(o)
                    End Function
                    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> Public Overrides Function GetHashCode() As Integer
                        Return MyBase.GetHashCode
                    End Function
                    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> _
                    Friend Overloads Function [GetType]() As Global.System.Type
                        Return GetType(MyForms)
                    End Function
                    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> Public Overrides Function ToString() As String
                        Return MyBase.ToString
                    End Function
                End Class

                Private m_MyFormsObjectProvider As New ThreadSafeObjectProvider(Of MyForms)

        *//* TODO ERROR: Skipped EndIfDirectiveTrivia
        #End If
        */
        /* TODO ERROR: Skipped IfDirectiveTrivia
        #If _MYWEBSERVICES = True Then
        */
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
                instance = default(T);
            }

            [DebuggerHidden()]
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public MyWebServices() : base()
            {
            }
        }

        private readonly static ThreadSafeObjectProvider<MyWebServices> m_MyWebServicesObjectProvider = new ThreadSafeObjectProvider<MyWebServices>();
        /* TODO ERROR: Skipped EndIfDirectiveTrivia
        #End If
        */
        /* TODO ERROR: Skipped IfDirectiveTrivia
        #If _MYTYPE = "Web" Then
        *//* TODO ERROR: Skipped DisabledTextTrivia

                <Global.System.ComponentModel.Design.HelpKeyword("My.Request")> _
                Friend ReadOnly Property Request() As Global.System.Web.HttpRequest
                    <Global.System.Diagnostics.DebuggerHidden()> _
                    Get
                        Dim CurrentContext As Global.System.Web.HttpContext = Global.System.Web.HttpContext.Current
                        If CurrentContext IsNot Nothing Then
                            Return CurrentContext.Request
                        End If
                        Return Nothing
                    End Get
                End Property

                <Global.System.ComponentModel.Design.HelpKeyword("My.Response")> _
                Friend ReadOnly Property Response() As Global.System.Web.HttpResponse
                    <Global.System.Diagnostics.DebuggerHidden()> _
                    Get
                        Dim CurrentContext As Global.System.Web.HttpContext = Global.System.Web.HttpContext.Current
                        If CurrentContext IsNot Nothing Then
                            Return CurrentContext.Response
                        End If
                        Return Nothing
                    End Get
                End Property

                <Global.System.ComponentModel.Design.HelpKeyword("My.Application.Log")> _
                Friend ReadOnly Property Log() As Global.Microsoft.VisualBasic.Logging.AspLog
                    <Global.System.Diagnostics.DebuggerHidden()> _
                    Get
                        Return m_LogObjectProvider.GetInstance()
                    End Get
                End Property

                Private ReadOnly m_LogObjectProvider As New ThreadSafeObjectProvider(Of Global.Microsoft.VisualBasic.Logging.AspLog)

        *//* TODO ERROR: Skipped EndIfDirectiveTrivia
        #End If  '_MYTYPE="Web"
        */
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Runtime.InteropServices.ComVisible(false)]
        internal sealed class ThreadSafeObjectProvider<T> where T : new()
        {
            internal T GetInstance
            {
                /* TODO ERROR: Skipped IfDirectiveTrivia
                #If TARGET = "library" Then
                */
                [DebuggerHidden()]
                get
                {
                    var Value = m_Context.Value;
                    if (Value is null)
                    {
                        Value = new T();
                        m_Context.Value = Value;
                    }
                    return Value;
                }
                /* TODO ERROR: Skipped ElseDirectiveTrivia
                #Else
                *//* TODO ERROR: Skipped DisabledTextTrivia
                                <Global.System.Diagnostics.DebuggerHidden()> _
                                Get
                                    If m_ThreadStaticValue Is Nothing Then m_ThreadStaticValue = New T
                                    Return m_ThreadStaticValue
                                End Get
                *//* TODO ERROR: Skipped EndIfDirectiveTrivia
                #End If
                */
            }

            [DebuggerHidden()]
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public ThreadSafeObjectProvider() : base()
            {
            }

            /* TODO ERROR: Skipped IfDirectiveTrivia
            #If TARGET = "library" Then
            */
            private readonly Microsoft.VisualBasic.MyServices.Internal.ContextValue<T> m_Context = new Microsoft.VisualBasic.MyServices.Internal.ContextValue<T>();
            /* TODO ERROR: Skipped ElseDirectiveTrivia
            #Else
            *//* TODO ERROR: Skipped DisabledTextTrivia
                        <Global.System.Runtime.CompilerServices.CompilerGenerated(), Global.System.ThreadStatic()> Private Shared m_ThreadStaticValue As T
            *//* TODO ERROR: Skipped EndIfDirectiveTrivia
            #End If
            */
        }
    }
}
/* TODO ERROR: Skipped EndIfDirectiveTrivia
#End If
*/