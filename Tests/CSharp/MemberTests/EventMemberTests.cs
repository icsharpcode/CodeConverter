using System.Threading.Tasks;
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
    ' Comment bug: This comment moves due to the Handles transformation
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
                // Comment bug: This comment moves due to the Handles transformation
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
        MyClass.New()
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
    static Class1()
    {
        SharedEventClassInstance = new MyEventClass();
    }

    public Class1()
    {
        NonSharedEventClassInstance = new MyEventClass();
    }

    public Class1(int num)
    {
        NonSharedEventClassInstance = new MyEventClass();
    }

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

    public Class1(object obj) : this()
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
}
1 source compilation errors:
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.", hasLineCommentConversionIssue: true);//TODO: Improve comment mapping for events
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
    public Class1(int num)
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
    }

    public Class1()
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
    }

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
    }
}
