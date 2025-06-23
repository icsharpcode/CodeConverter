using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class EventMemberTests : ConverterTestBase
{

    [Fact]
    public async Task TestEventAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    public event EventHandler MyEvent;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestSharedEventAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    public static event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler(string a);
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestEventWithNoDeclaredTypeOrHandlersAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class TestEventWithNoType
{
    public event OnCakeChangeEventHandler OnCakeChange;

    public delegate void OnCakeChangeEventHandler();

    public void RaisingFlour()
    {
        OnCakeChange?.Invoke();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestEventsOnInterfaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.IO;

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

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestModuleHandlesWithEventsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestWithEventsWithoutInitializerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestClassHandlesWithEventsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestPartialClassHandlesWithEventsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestInitializeComponentAddsEventHandlersAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

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
CS0246: The type or namespace name 'Button' could not be found (are you missing a using directive or an assembly reference?)", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue774_HandlerForBasePropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue584_EventWithByRefAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

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
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue967_HandlerAssignmentShouldComeLastInConstructorAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Windows.Forms;
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
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue991_EventAssignmentRuntimeNullRefAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Runtime.CompilerServices;

public static partial class Program
{
    public static void Main(string[] args)
    {
        var c = new SomeClass(new SomeDependency());
        Console.WriteLine(""Done"");
    }
}

public partial class SomeDependency
{
    public event EventHandler SomeEvent;
}

public partial class SomeClass
{
    private SomeDependency __dep;

    private SomeDependency _dep
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return __dep;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (__dep != null)
            {
                __dep.SomeEvent -= _dep_SomeEvent;
            }

            __dep = value;
            if (__dep != null)
            {
                __dep.SomeEvent += _dep_SomeEvent;
            }
        }
    }

    public SomeClass(object dep)
    {
        _dep = (SomeDependency)dep;
    }

    private void _dep_SomeEvent(object sender, EventArgs e)
    {
        // Do Something
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Test_Issue701_MultiLineHandlesSyntaxAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }
}