using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.CSharp;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests
{
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
End Module", @"using System.Runtime.CompilerServices;

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
    static Module1()
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
    }

    private static MyEventClass _EventClassInstance, _EventClassInstance2;

    private static MyEventClass EventClassInstance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _EventClassInstance;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_EventClassInstance != null)
            {
                _EventClassInstance.TestEvent -= PrintTestMessage2;
                _EventClassInstance.TestEvent -= PrintTestMessage3;
            }

            _EventClassInstance = value;
            if (_EventClassInstance != null)
            {
                _EventClassInstance.TestEvent += PrintTestMessage2;
                _EventClassInstance.TestEvent += PrintTestMessage3;
            }
        }
    }

    private static MyEventClass EventClassInstance2
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _EventClassInstance2;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_EventClassInstance2 != null)
            {
                _EventClassInstance2.TestEvent -= PrintTestMessage2;
            }

            _EventClassInstance2 = value;
            if (_EventClassInstance2 != null)
            {
                _EventClassInstance2.TestEvent += PrintTestMessage2;
            }
        }
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
End Class", @"using System.Runtime.CompilerServices;

internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();
}

internal partial class Class1
{
    private MyEventClass _MyEventClassInstance;

    private MyEventClass MyEventClassInstance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _MyEventClassInstance;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_MyEventClassInstance != null)
            {
                _MyEventClassInstance.TestEvent -= EventClassInstance_TestEvent;
            }

            _MyEventClassInstance = value;
            if (_MyEventClassInstance != null)
            {
                _MyEventClassInstance.TestEvent += EventClassInstance_TestEvent;
            }
        }
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
    WithEvents NonSharedEventClassInstance As New MyEventClass

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
End Class", @"using System.Runtime.CompilerServices;

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
    private static MyEventClass _SharedEventClassInstance;

    private static MyEventClass SharedEventClassInstance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _SharedEventClassInstance;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_SharedEventClassInstance != null)
            {
                _SharedEventClassInstance.TestEvent -= PrintTestMessage2;
            }

            _SharedEventClassInstance = value;
            if (_SharedEventClassInstance != null)
            {
                _SharedEventClassInstance.TestEvent += PrintTestMessage2;
            }
        }
    }

    private MyEventClass _NonSharedEventClassInstance;

    private MyEventClass NonSharedEventClassInstance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _NonSharedEventClassInstance;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_NonSharedEventClassInstance != null)
            {
                _NonSharedEventClassInstance.TestEvent -= PrintTestMessage2;
                _NonSharedEventClassInstance.TestEvent -= PrintTestMessage3;
            }

            _NonSharedEventClassInstance = value;
            if (_NonSharedEventClassInstance != null)
            {
                _NonSharedEventClassInstance.TestEvent += PrintTestMessage2;
                _NonSharedEventClassInstance.TestEvent += PrintTestMessage3;
            }
        }
    }

    static Class1()
    {
        SharedEventClassInstance = new MyEventClass();
    }

    public Class1(int num)
    {
        NonSharedEventClassInstance = new MyEventClass();
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
}", hasLineCommentConversionIssue: true);//TODO: Improve comment mapping for events
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
    WithEvents EventClassInstance, EventClassInstance2 As New MyEventClass

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
End Class", @"using System.Runtime.CompilerServices;

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
    private MyEventClass _EventClassInstance, _EventClassInstance2;

    private MyEventClass EventClassInstance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _EventClassInstance;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_EventClassInstance != null)
            {
                _EventClassInstance.TestEvent -= PrintTestMessage2;
                _EventClassInstance.TestEvent -= PrintTestMessage3;
            }

            _EventClassInstance = value;
            if (_EventClassInstance != null)
            {
                _EventClassInstance.TestEvent += PrintTestMessage2;
                _EventClassInstance.TestEvent += PrintTestMessage3;
            }
        }
    }

    private MyEventClass EventClassInstance2
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _EventClassInstance2;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_EventClassInstance2 != null)
            {
                _EventClassInstance2.TestEvent -= PrintTestMessage2;
            }

            _EventClassInstance2 = value;
            if (_EventClassInstance2 != null)
            {
                _EventClassInstance2.TestEvent += PrintTestMessage2;
            }
        }
    }

    public Class1()
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
    }

    public Class1(int num)
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
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
}", hasLineCommentConversionIssue: true);//TODO: Improve comment mapping for events
        }

        [Fact]
        public async Task TestInitializeComponentAddsEventHandlersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports Microsoft.VisualBasic.CompilerServices

<DesignerGenerated>
Partial Public Class TestHandlesAdded

    Sub InitializeComponent()
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
End Class", @"using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

[DesignerGenerated]
public partial class TestHandlesAdded
{
    public TestHandlesAdded()
    {
        InitializeComponent();
        _POW_btnV2DBM.Name = ""POW_btnV2DBM"";
    }

    public void InitializeComponent()
    {
        // 
        // POW_btnV2DBM
        // 
        _POW_btnV2DBM.Location = new System.Drawing.Point(207, 15);
        _POW_btnV2DBM.Name = ""_POW_btnV2DBM"";
        _POW_btnV2DBM.Size = new System.Drawing.Size(42, 23);
        _POW_btnV2DBM.TabIndex = 3;
        _POW_btnV2DBM.Text = "">>"";
        _POW_btnV2DBM.UseVisualStyleBackColor = true;
    }
}

public partial class TestHandlesAdded
{
    private Button _POW_btnV2DBM;

    private Button POW_btnV2DBM
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _POW_btnV2DBM;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_POW_btnV2DBM != null)
            {
                _POW_btnV2DBM.Click -= POW_btnV2DBM_Click;
            }

            _POW_btnV2DBM = value;
            if (_POW_btnV2DBM != null)
            {
                _POW_btnV2DBM.Click += POW_btnV2DBM_Click;
            }
        }
    }

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

    internal System.Windows.Forms.Button Button1
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

    internal System.Windows.Forms.Button Button2
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
}
