using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class EventMemberTests : ConverterTestBase
{

    [Fact]
    public async Task TestEventAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass
    Public Event MyEvent As EventHandler
End Class", @"using System;

internal partial class TestClass
{
    public event EventHandler MyEvent;
}");
    }

    [Fact]
    public async Task TestSharedEventAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass
    Public Shared Event TestEvent(a As String)
End Class", @"
internal partial class TestClass
{
    public static event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler(string a);
}");
    }

    [Fact]
    public async Task TestEventWithNoDeclaredTypeOrHandlersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class TestEventWithNoType
    Public Event OnCakeChange

    Public Sub RaisingFlour()
        RaiseEvent OnCakeChange
    End Sub
End Class", @"
public partial class TestEventWithNoType
{
    public event OnCakeChangeEventHandler OnCakeChange;

    public delegate void OnCakeChangeEventHandler();

    public void RaisingFlour()
    {
        OnCakeChange?.Invoke();
    }
}");
    }

    [Fact]
    public async Task TestEventsOnInterfaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Interface IFileSystem

    Event FileChanged(FileData As String)
    Event FileCreated(FileData As String)
    Event FileDeleted(FileData As String)
    Event FileRenamed(e As RenamedEventArgs)
    Event WatcherError(e As ErrorEventArgs)

End Interface

Public Class FileSystemWin
    Implements IFileSystem

    Public Event FileChanged(FileData As String) Implements IFileSystem.FileChanged
    Public Event FileCreated(FileData As String) Implements IFileSystem.FileCreated
    Public Event FileDeleted(FileData As String) Implements IFileSystem.FileDeleted
    Public Event FileRenamed(e As RenamedEventArgs) Implements IFileSystem.FileRenamed
    Public Event WatcherError(e As ErrorEventArgs) Implements IFileSystem.WatcherError

End Class", @"using System.IO;

public partial interface IFileSystem
{

    event FileChangedEventHandler FileChanged;

    delegate void FileChangedEventHandler(string FileData);
    event FileCreatedEventHandler FileCreated;

    delegate void FileCreatedEventHandler(string FileData);
    event FileDeletedEventHandler FileDeleted;

    delegate void FileDeletedEventHandler(string FileData);
    event FileRenamedEventHandler FileRenamed;

    delegate void FileRenamedEventHandler(RenamedEventArgs e);
    event WatcherErrorEventHandler WatcherError;

    delegate void WatcherErrorEventHandler(ErrorEventArgs e);

}

public partial class FileSystemWin : IFileSystem
{

    public event IFileSystem.FileChangedEventHandler FileChanged;
    public event IFileSystem.FileCreatedEventHandler FileCreated;
    public event IFileSystem.FileDeletedEventHandler FileDeleted;
    public event IFileSystem.FileRenamedEventHandler FileRenamed;
    public event IFileSystem.WatcherErrorEventHandler WatcherError;

}");
    }

    [Fact]
    public async Task TestModuleHandlesWithEventsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class MyEventClass
    Public Event TestEvent()

    Sub RaiseEvents()
        RaiseEvent TestEvent()
    End Sub
End Class

Module Module1
    WithEvents EventClassInstance, EventClassInstance2 As New MyEventClass

    Sub PrintTestMessage2() Handles EventClassInstance.TestEvent, EventClassInstance2.TestEvent
    End Sub

    Sub PrintTestMessage3() Handles EventClassInstance.TestEvent
    End Sub
End Module", @"
internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();

    public void RaiseEvents()
    {
        TestEvent?.Invoke();
    }
}

internal static partial class Module1
{
    private static MyEventClass EventClassInstance, EventClassInstance2;

    static Module1()
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
        EventClassInstance.TestEvent += PrintTestMessage2;
        EventClassInstance.TestEvent += PrintTestMessage3;
        EventClassInstance2.TestEvent += PrintTestMessage2;
    }

    public static void PrintTestMessage2()
    {
    }

    public static void PrintTestMessage3()
    {
    }
}");
    }

    [Fact]
    public async Task TestWithEventsWithoutInitializerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class MyEventClass
    Public Event TestEvent()
End Class
Class Class1
    WithEvents MyEventClassInstance As MyEventClass
    Sub EventClassInstance_TestEvent() Handles MyEventClassInstance.TestEvent
    End Sub
End Class", @"
internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();
}

internal partial class Class1
{
    private MyEventClass MyEventClassInstance;

    public Class1()
    {
        MyEventClassInstance.TestEvent += EventClassInstance_TestEvent;
    }
    public void EventClassInstance_TestEvent()
    {
    }
}
");
    }

    [Fact]
    public async Task TestClassHandlesWithEventsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class MyEventClass
    Public Event TestEvent()

    Sub RaiseEvents()
        RaiseEvent TestEvent()
    End Sub
End Class

Class Class1
    Shared WithEvents SharedEventClassInstance As New MyEventClass
    WithEvents NonSharedEventClassInstance As New MyEventClass 'Comment moves to initialization in c# constructor

    Public Sub New(num As Integer)
    End Sub

    Public Sub New(obj As Object)
        MyClass.New(7)
    End Sub

    Shared Sub PrintTestMessage2() Handles SharedEventClassInstance.TestEvent, NonSharedEventClassInstance.TestEvent
    End Sub

    Sub PrintTestMessage3() Handles NonSharedEventClassInstance.TestEvent
    End Sub

    Public Class NestedShouldNotGainConstructor
    End Class
End Class

Public Class ShouldNotGainConstructor
End Class", @"
internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();

    public void RaiseEvents()
    {
        TestEvent?.Invoke();
    }
}

internal partial class Class1
{
    private static MyEventClass SharedEventClassInstance;
    private MyEventClass NonSharedEventClassInstance;

    static Class1()
    {
        SharedEventClassInstance = new MyEventClass();
        SharedEventClassInstance.TestEvent += PrintTestMessage2;
    }

    public Class1(int num)
    {
        NonSharedEventClassInstance = new MyEventClass(); // Comment moves to initialization in c# constructor
        NonSharedEventClassInstance.TestEvent += PrintTestMessage2;
        NonSharedEventClassInstance.TestEvent += PrintTestMessage3;
    }

    public Class1(object obj) : this(7)
    {
    }

    public static void PrintTestMessage2()
    {
    }

    public void PrintTestMessage3()
    {
    }

    public partial class NestedShouldNotGainConstructor
    {
    }
}

public partial class ShouldNotGainConstructor
{
}");
    }

    [Fact]
    public async Task TestPartialClassHandlesWithEventsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class MyEventClass
    Public Event TestEvent()

    Sub RaiseEvents()
        RaiseEvent TestEvent()
    End Sub
End Class

Partial Class Class1
    WithEvents EventClassInstance, EventClassInstance2 As New MyEventClass 'Comment moves to initialization in c# constructor

    Public Sub New()
    End Sub

    Public Sub New(num As Integer)
    End Sub

    Public Sub New(obj As Object)
        MyClass.New()
    End Sub
End Class

Public Partial Class Class1
    Sub PrintTestMessage2() Handles EventClassInstance.TestEvent, EventClassInstance2.TestEvent
    End Sub

    Sub PrintTestMessage3() Handles EventClassInstance.TestEvent
    End Sub
End Class", @"
internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();

    public void RaiseEvents()
    {
        TestEvent?.Invoke();
    }
}

public partial class Class1
{
    private MyEventClass EventClassInstance, EventClassInstance2;

    public Class1()
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
        EventClassInstance.TestEvent += PrintTestMessage2;
        EventClassInstance.TestEvent += PrintTestMessage3;
        EventClassInstance2.TestEvent += PrintTestMessage2;
    }

    public Class1(int num)
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass(); // Comment moves to initialization in c# constructor
        EventClassInstance.TestEvent += PrintTestMessage2;
        EventClassInstance.TestEvent += PrintTestMessage3;
        EventClassInstance2.TestEvent += PrintTestMessage2;
    }

    public Class1(object obj) : this()
    {
    }
}

public partial class Class1
{
    public void PrintTestMessage2()
    {
    }

    public void PrintTestMessage3()
    {
    }
}");
    }

    [Fact]
    public async Task TestInitializeComponentAddsEventHandlersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports Microsoft.VisualBasic.CompilerServices

<DesignerGenerated>
Partial Public Class TestHandlesAdded

    Sub InitializeComponent()
        Me.POW_btnV2DBM = New System.Windows.Forms.Button()
        '
        'POW_btnV2DBM
        '
        Me.POW_btnV2DBM.Location = New System.Drawing.Point(207, 15)
        Me.POW_btnV2DBM.Name = ""POW_btnV2DBM""
        Me.POW_btnV2DBM.Size = New System.Drawing.Size(42, 23)
        Me.POW_btnV2DBM.TabIndex = 3
        Me.POW_btnV2DBM.Text = "">>""
        Me.POW_btnV2DBM.UseVisualStyleBackColor = True
    End Sub

End Class

Partial Public Class TestHandlesAdded
    Dim WithEvents POW_btnV2DBM As Button

    Public Sub POW_btnV2DBM_Click() Handles POW_btnV2DBM.Click

    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

[DesignerGenerated]
public partial class TestHandlesAdded
{
    public TestHandlesAdded()
    {
        InitializeComponent();
    }

    public void InitializeComponent()
    {
        POW_btnV2DBM = new System.Windows.Forms.Button();
        POW_btnV2DBM.Click += POW_btnV2DBM_Click;
        // 
        // POW_btnV2DBM
        // 
        POW_btnV2DBM.Location = new System.Drawing.Point(207, 15);
        POW_btnV2DBM.Name = ""POW_btnV2DBM"";
        POW_btnV2DBM.Size = new System.Drawing.Size(42, 23);
        POW_btnV2DBM.TabIndex = 3;
        POW_btnV2DBM.Text = "">>"";
        POW_btnV2DBM.UseVisualStyleBackColor = true;
    }

}

public partial class TestHandlesAdded
{
    private Button POW_btnV2DBM;

    public void POW_btnV2DBM_Click()
    {

    }
}
2 source compilation errors:
BC30002: Type 'Button' is not defined.
BC30590: Event 'Click' cannot be found.
1 target compilation errors:
CS0246: The type or namespace name 'Button' could not be found (are you missing a using directive or an assembly reference?)");
    }

    [Fact]
    public async Task Issue774_HandlerForBasePropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.CompilerServices

Partial Class BaseForm
    Inherits Form
    Friend WithEvents BaseButton As Button
End Class

<DesignerGenerated>
Partial Class BaseForm
    Inherits System.Windows.Forms.Form

    Private Sub InitializeComponent()
        Me.BaseButton = New Button()
    End Sub
End Class

<DesignerGenerated>
Partial Class Form1
    Inherits BaseForm
    Private Sub InitializeComponent()
        Me.Button1 = New Button()
    End Sub
    Friend WithEvents Button1 As Button
End Class

Partial Class Form1
    Private Sub MultiClickHandler(sender As Object, e As EventArgs) Handles Button1.Click,
                                                                            BaseButton.Click
    End Sub
End Class", @"using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class BaseForm : Form
{
    private Button _BaseButton;

    internal virtual Button BaseButton
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _BaseButton;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            _BaseButton = value;
        }
    }

    public BaseForm()
    {
        InitializeComponent();
        BaseButton = _BaseButton;
    }
}

[DesignerGenerated]
internal partial class BaseForm : Form
{

    private void InitializeComponent()
    {
        _BaseButton = new Button();
    }
}

[DesignerGenerated]
internal partial class Form1 : BaseForm
{
    internal override Button BaseButton
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return base.BaseButton;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (base.BaseButton != null)
            {
                base.BaseButton.Click -= MultiClickHandler;
            }

            base.BaseButton = value;
            if (base.BaseButton != null)
            {
                base.BaseButton.Click += MultiClickHandler;
            }
        }
    }

    public Form1()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        Button1 = new Button();
        Button1.Click += new EventHandler(MultiClickHandler);
    }
    internal Button Button1;
}

internal partial class Form1
{
    private void MultiClickHandler(object sender, EventArgs e)
    {
    }
}
");
    }

    [Fact]
    public async Task Issue584_EventWithByRefAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Issue584RaiseEventByRefDemo
    Public Event ConversionNeeded(ai_OrigID As Integer, ByRef NewID As Integer)

    Public Function TestConversion(ai_ID) As Integer
        Dim i_NewValue As Integer
        RaiseEvent ConversionNeeded(ai_ID, i_NewValue)
        Return i_NewValue
    End Function
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Issue584RaiseEventByRefDemo
{
    public event ConversionNeededEventHandler ConversionNeeded;

    public delegate void ConversionNeededEventHandler(int ai_OrigID, ref int NewID);

    public int TestConversion(object ai_ID)
    {
        var i_NewValue = default(int);
        ConversionNeeded?.Invoke(Conversions.ToInteger(ai_ID), ref i_NewValue);
        return i_NewValue;
    }
}
");
    }

    [Fact]
    public async Task Issue967_HandlerAssignmentShouldComeLastInConstructorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Windows
Imports System.Windows.Forms

Public Partial Class MainWindow
    Inherits Form
    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub MainWindow_Loaded() Handles MyBase.Load
        Interaction.MsgBox(""Window, loaded"")
    End Sub
End Class

Public Partial Class MainWindow
    Public Sub InitializeComponent()
    End Sub
End Class
", @"using System.Windows.Forms;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class MainWindow : Form
{
    public MainWindow()
    {
        InitializeComponent();
        Load += (_, __) => MainWindow_Loaded();
    }

    private void MainWindow_Loaded()
    {
        Interaction.MsgBox(""Window, loaded"");
    }
}

public partial class MainWindow
{
    public void InitializeComponent()
    {
    }
}
");
    }

    [Fact]
    public async Task Test_Issue701_MultiLineHandlesSyntaxAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Form1
    Private Sub MultiClickHandler(sender As Object, e As EventArgs) Handles Button1.Click,
                                                                            Button2.Click
    End Sub
End Class

Partial Class Form1
    Inherits System.Windows.Forms.Form

    Private Sub InitializeComponent()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
    End Sub

    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Button2 As System.Windows.Forms.Button
End Class",
            @"using System;
using System.Runtime.CompilerServices;

public partial class Form1
{
    private void MultiClickHandler(object sender, EventArgs e)
    {
    }
}

public partial class Form1 : System.Windows.Forms.Form
{

    private void InitializeComponent()
    {
        _Button1 = new System.Windows.Forms.Button();
        _Button1.Click += new EventHandler(MultiClickHandler);
        _Button2 = new System.Windows.Forms.Button();
        _Button2.Click += new EventHandler(MultiClickHandler);
    }

    private System.Windows.Forms.Button _Button1;

    internal virtual System.Windows.Forms.Button Button1
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _Button1;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_Button1 != null)
            {
                _Button1.Click -= MultiClickHandler;
            }

            _Button1 = value;
            if (_Button1 != null)
            {
                _Button1.Click += MultiClickHandler;
            }
        }
    }
    private System.Windows.Forms.Button _Button2;

    internal virtual System.Windows.Forms.Button Button2
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _Button2;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_Button2 != null)
            {
                _Button2.Click -= MultiClickHandler;
            }

            _Button2 = value;
            if (_Button2 != null)
            {
                _Button2.Click += MultiClickHandler;
            }
        }
    }
}");
    }
}