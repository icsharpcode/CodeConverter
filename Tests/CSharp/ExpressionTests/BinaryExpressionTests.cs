using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class BinaryExpressionTests : ConverterTestBase
{
    [Fact]
    public async Task OmitsConversionForEnumBinaryExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Friend Enum RankEnum As SByte
    First = 1
    Second = 2
End Enum

Public Class TestClass
    Sub TestMethod()
        Dim eEnum = RankEnum.Second
        Dim enumEnumEquality As Boolean = eEnum = RankEnum.First
    End Sub
End Class", @"
internal enum RankEnum : sbyte
{
    First = 1,
    Second = 2
}

public partial class TestClass
{
    public void TestMethod()
    {
        var eEnum = RankEnum.Second;
        bool enumEnumEquality = eEnum == RankEnum.First;
    }
}");
    }

    [Fact]
    public async Task BinaryOperatorsIsIsNotLeftShiftRightShiftAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private bIs as Boolean = New Object Is New Object
    Private bIsNot as Boolean = New Object IsNot New Object
    Private bLeftShift as Integer = 1 << 3
    Private bRightShift as Integer = 8 >> 3
End Class", @"
internal partial class TestClass
{
    private bool bIs = ReferenceEquals(new object(), new object());
    private bool bIsNot = !ReferenceEquals(new object(), new object());
    private int bLeftShift = 1 << 3;
    private int bRightShift = 8 >> 3;
}");
    }

    [Fact]
    public async Task LikeOperatorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Foo()
        Dim x = """" Like ""*x*""
    End Sub
End Class", @"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        bool x = LikeOperator.LikeString("""", ""*x*"", CompareMethod.Binary);
    }
}");
    }

    [Fact]
    public async Task ShiftAssignmentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 1
        x <<= 4
        x >>= 3
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int x = 1;
        x <<= 4;
        x >>= 3;
    }
}");
    }

    [Fact]
    public async Task IntegerArithmeticAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 7 ^ 6 Mod 5 \ 4 + 3 * 2
        x += 1
        x -= 2
        x *= 3
        x \= 4
        x ^= 5
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        double x = Math.Pow(7d, 6d) % (5 / 4) + 3 * 2;
        x += 1d;
        x -= 2d;
        x *= 3d;
        x = (double)(x / 4L);
        x = Math.Pow(x, 5d);
    }
}");
    }

    [Fact]
    public async Task ImplicitConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x As Double = 1
        Dim y As Decimal = 2
        Dim i1 As Integer = 1
        Dim i2 As Integer = 2
        Dim d1 = i1 / i2
        Dim z = x + y
        Dim z2 = y + x
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        double x = 1d;
        decimal y = 2m;
        int i1 = 1;
        int i2 = 2;
        double d1 = i1 / (double)i2;
        double z = x + (double)y;
        double z2 = (double)y + x;
    }
}
");
    }

    [Fact]
    public async Task FloatingPointDivisionIsForcedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 10 / 3
        x /= 2
        Dim y = 10.0 / 3
        y /= 2
        Dim z As Integer = 8
        z /= 3
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        double x = 10d / 3d;
        x /= 2d;
        double y = 10.0d / 3d;
        y /= 2d;
        int z = 8;
        z = (int)Math.Round(z / 3d);
    }
}");
    }

    [Fact]
    public async Task ConditionalExpressionInBinaryExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Integer = 5 - If((str = """"), 1, 2)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        int result = 5 - (string.IsNullOrEmpty(str) ? 1 : 2);
    }
}");
    }

    [Fact]
        public async Task NotOperatorOnNullableBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass712
    Private Function TestMethod() As Integer
        Dim x As Boolean? = Nothing
        If Not x Then Return 1 Else Return 2
    End Function
End Class", @"
internal partial class TestClass712
{
    private int TestMethod()
    {
        bool? x = default;
        if (x == false)
            return 1;
        else
            return 2;
    }
}");
        }

        [Fact]
        public async Task AndOperatorOnNullableBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim a As Boolean? = Nothing
        Dim b As Boolean? = Nothing
        Dim x As Boolean = False

        If a And b Then Return
        If a AndAlso b Then Return
        If a And x Then Return
        If a AndAlso x Then Return
        If x And a Then Return
        If x AndAlso a Then Return

        Dim res As Boolean? = a And b
        res = a AndAlso b
        res = a And x
        res = a AndAlso x 
        res = x And a
        res = x AndAlso a 
        
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        bool? a = default;
        bool? b = default;
        bool x = false;

        if ((a & b) == true)
            return;
        if ((a.HasValue && !a.Value ? false : !b.HasValue ? null : b.Value ? a : false) == true)
            return;
        if ((a & x) == true)
            return;
        if ((!a.HasValue || a.Value) && x && a.HasValue)
            return;
        if ((x & a) == true)
            return;
        if (x && a.GetValueOrDefault())
            return;

        var res = a & b;
        res = a.HasValue && !a.Value ? false : !b.HasValue ? null : b.Value ? a : false;
        res = a & x;
        res = a.HasValue && !a.Value ? false : x ? a : false;
        res = x & a;
        res = x ? a : false;

    }
}");
        }

        [Fact]
        public async Task OrOperatorOnNullableBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim a As Boolean? = Nothing
        Dim b As Boolean? = Nothing
        Dim x As Boolean = False

        If a Or b Then Return
        If a OrElse b Then Return
        If a Or x Then Return
        If a OrElse x Then Return
        If x Or a Then Return
        If x OrElse a Then Return

        Dim res As Boolean? = a Or b
        res = a OrElse b
        res = a Or x
        res = a OrElse x 
        res = x Or a
        res = x OrElse a 
        
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        bool? a = default;
        bool? b = default;
        bool x = false;

        if ((a | b) == true)
            return;
        if (a.GetValueOrDefault() || b.GetValueOrDefault())
            return;
        if ((a | x) == true)
            return;
        if (a.GetValueOrDefault() || x)
            return;
        if ((x | a) == true)
            return;
        if (x || a.GetValueOrDefault())
            return;

        var res = a | b;
        res = a.GetValueOrDefault() ? true : b is not { } arg1 ? null : arg1 ? true : a;
        res = a | x;
        res = a.GetValueOrDefault() ? true : x ? true : a;
        res = x | a;
        res = x ? true : a;

    }
}");
        }

        [Fact]
        public async Task RelationalOperatorsOnNullableTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x As Integer? = Nothing
        Dim y As Integer? = Nothing
        Dim a As Integer = 0

        Dim res As Boolean? = x = y
        res = x <> y
        res = x > y
        res = x >= y
        res = x < y
        res = x <= y

        res = a = y
        res = a <> y
        res = a > y
        res = a >= y
        res = a < y
        res = a <= y

        res = x = a
        res = x <> a
        res = x > a
        res = x >= a
        res = x < a
        res = x <= a
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int? x = default;
        int? y = default;
        int a = 0;

        var res = x.HasValue && y.HasValue ? x.Value == y.Value : (bool?)null;
        res = x.HasValue && y.HasValue ? x.Value != y.Value : null;
        res = x.HasValue && y.HasValue ? x.Value > y.Value : null;
        res = x.HasValue && y.HasValue ? x.Value >= y.Value : null;
        res = x.HasValue && y.HasValue ? x.Value < y.Value : null;
        res = x.HasValue && y.HasValue ? x.Value <= y.Value : null;

        res = y.HasValue ? a == y.Value : null;
        res = y.HasValue ? a != y.Value : null;
        res = y.HasValue ? a > y.Value : null;
        res = y.HasValue ? a >= y.Value : null;
        res = y.HasValue ? a < y.Value : null;
        res = y.HasValue ? a <= y.Value : null;

        res = x.HasValue ? x.Value == a : null;
        res = x.HasValue ? x.Value != a : null;
        res = x.HasValue ? x.Value > a : null;
        res = x.HasValue ? x.Value >= a : null;
        res = x.HasValue ? x.Value < a : null;
        res = x.HasValue ? x.Value <= a : null;
    }
}");
        }

        [Fact]
        public async Task SimplifiesAlreadyCheckedNullableComparison_HasValueAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return newDays.HasValue AndAlso oldDays.HasValue AndAlso newDays <> oldDays
End Function
", @"
private bool TestMethod(int? newDays, int? oldDays)
{
    return newDays.HasValue && oldDays.HasValue && newDays != oldDays;
}");
    }

        [Fact]
        public async Task SimplifiesAlreadyCheckedNullableComparison_NotNothingAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return newDays IsNot Nothing AndAlso oldDays IsNot Nothing AndAlso newDays = oldDays
End Function
", @"
private bool TestMethod(int? newDays, int? oldDays)
{
    return newDays is not null && oldDays is not null && newDays == oldDays;
}");
        }

        [Fact]
        public async Task DoesNotSimplifyComparisonWhenNullChecksAreNotDefinitelyTrueAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return (newDays.HasValue AndAlso oldDays.HasValue OrElse True) AndAlso newDays > oldDays
End Function
", @"
private bool TestMethod(int? newDays, int? oldDays)
{
    return (bool)(newDays.HasValue && oldDays.HasValue || true ? newDays.HasValue && oldDays.HasValue ? newDays.Value > oldDays.Value : null : (bool?)false);
}");
        }

        [Fact]
        public async Task HalfSimplifiesComparisonWhenOneSideAlreadyNullCheckedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return newDays.HasValue AndAlso newDays < oldDays
End Function
", @"
private bool TestMethod(int? newDays, int? oldDays)
{
    return (bool)(newDays.HasValue ? oldDays.HasValue ? newDays < oldDays.Value : null : (bool?)false);
}");
    }

        [Fact]
        public async Task DoesNotSimplifyComparisonWhenNullableChecksAreUncertainAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return (newDays.HasValue OrElse oldDays.HasValue) AndAlso newDays <> oldDays
End Function
", @"
private bool TestMethod(int? newDays, int? oldDays)
{
    return (bool)(newDays.HasValue || oldDays.HasValue ? newDays.HasValue && oldDays.HasValue ? newDays.Value != oldDays.Value : null : (bool?)false);
}");
        }

        [Fact]
        public async Task SimplifiesNullableEnumIfEqualityCheckAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Public Enum PasswordStatus
    Expired
    Locked    
End Enum
Public Class TestForEnums
    Public Shared Sub WriteStatus(status As PasswordStatus?)
      If status = PasswordStatus.Locked Then
          Console.Write(""Locked"")
      End If
    End Sub
End Class
", @"using System;

public enum PasswordStatus
{
    Expired,
    Locked
}

public partial class TestForEnums
{
    public static void WriteStatus(PasswordStatus? status)
    {
        if (status.HasValue && status.Value == PasswordStatus.Locked)
        {
            Console.Write(""Locked"");
        }
    }
}");
        }

        [Fact]
        public async Task SimplifiesNullableDateIfEqualityCheckAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Public Class TestForDates
    Public Shared Sub WriteStatus(adminDate As DateTime?, chartingTimeAllowanceEnd As DateTime)
        If adminDate Is Nothing OrElse adminDate > chartingTimeAllowanceEnd Then
            adminDate = DateTime.Now
        End If
    End Sub
End Class
", @"using System;

public partial class TestForDates
{
    public static void WriteStatus(DateTime? adminDate, DateTime chartingTimeAllowanceEnd)
    {
        if (adminDate is null || adminDate > chartingTimeAllowanceEnd)
        {
            adminDate = DateTime.Now;
        }
    }
}");
        }

        [Fact]
        public async Task RelationalOperatorsOnNullableTypeInConditionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x As Integer? = Nothing
        Dim y As Integer? = Nothing
        Dim a As Integer = 0

        If x = y Then Return
        If x <> y Then Return
        If x > y Then Return
        If x >= y Then Return
        If x < y Then Return
        If x <= y Then Return

        If a = y Then Return
        If a <> y Then Return
        If a > y Then Return
        If a >= y Then Return
        If a < y Then Return
        If a <= y Then Return

        IF x = a Then Return
        IF x <> a Then Return
        IF x > a Then Return
        IF x >= a Then Return
        IF x < a Then Return
        IF x <= a Then Return
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int? x = default;
        int? y = default;
        int a = 0;

        if (x.HasValue && y.HasValue && x.Value == y.Value)
            return;
        if (x.HasValue && y.HasValue && x.Value != y.Value)
            return;
        if (x.HasValue && y.HasValue && x.Value > y.Value)
            return;
        if (x.HasValue && y.HasValue && x.Value >= y.Value)
            return;
        if (x.HasValue && y.HasValue && x.Value < y.Value)
            return;
        if (x.HasValue && y.HasValue && x.Value <= y.Value)
            return;

        if (y.HasValue && a == y.Value)
            return;
        if (y.HasValue && a != y.Value)
            return;
        if (y.HasValue && a > y.Value)
            return;
        if (y.HasValue && a >= y.Value)
            return;
        if (y.HasValue && a < y.Value)
            return;
        if (y.HasValue && a <= y.Value)
            return;

        if (x.HasValue && x.Value == a)
            return;
        if (x.HasValue && x.Value != a)
            return;
        if (x.HasValue && x.Value > a)
            return;
        if (x.HasValue && x.Value >= a)
            return;
        if (x.HasValue && x.Value < a)
            return;
        if (x.HasValue && x.Value <= a)
            return;
    }
}");
        }

        [Fact]
        public async Task NullableBooleansComparedIssue982Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Dim newDays As Integer? = 1
Dim oldDays As Integer? = Nothing

If (newDays.HasValue AndAlso Not oldDays.HasValue) _
                OrElse (newDays.HasValue AndAlso oldDays.HasValue AndAlso newDays <> oldDays) _
                OrElse (Not newDays.HasValue AndAlso oldDays.HasValue) Then

'Some code
End If", @"{
    int? newDays = 1;
    int? oldDays = default;

    if (newDays.HasValue && !oldDays.HasValue || newDays.HasValue && oldDays.HasValue && newDays != oldDays || !newDays.HasValue && oldDays.HasValue)

    {

        // Some code
    }
}");
        }

        [Fact]
        public async Task NullableBooleanComparedToNormalBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim var1 As Boolean? = Nothing
        Dim a = var1 = False
        Dim b = var1 = True
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        bool? var1 = default;
        var a = var1.HasValue ? var1.Value == false : (bool?)null;
        var b = var1.HasValue ? var1.Value == true : (bool?)null;
    }
}");
        }

        [Fact]
        public async Task ImplicitBooleanConversion712Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass712
    Private Function TestMethod()
        Dim var1 As Boolean? = Nothing
        Dim var2 As Boolean? = Nothing
        Return var1 OrElse Not var2
    End Function
End Class", @"
internal partial class TestClass712
{
    private object TestMethod()
    {
        bool? var1 = default;
        bool? var2 = default;
        return (object)(var1.GetValueOrDefault() ? true : !var2 is not { } arg1 ? null : arg1 ? true : var1);
    }
}");
    }

    [Fact]
    public async Task ImplicitIfStatementBooleanConversion712Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass712
    Private Function TestMethod()
        Dim var1 As Boolean? = Nothing
        Dim var2 As Boolean? = Nothing
        If var1 OrElse Not var2 Then Return True Else Return False
    End Function
End Class", @"
internal partial class TestClass712
{
    private object TestMethod()
    {
        bool? var1 = default;
        bool? var2 = default;
        if (var1.GetValueOrDefault() || (!var2).GetValueOrDefault())
            return true;
        else
            return false;
    }
}");
    }

    [Fact]
    public async Task ConversionInComparisonOperatorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class ConversionInComparisonOperatorTest
    Public Sub Foo()
        Dim SomeDecimal As Decimal = 12.3
        Dim ACalc As Double = 32.1
        If ACalc > 60 / SomeDecimal Then
            Console.WriteLine(1)
        End If
    End Sub
End Class", @"using System;

public partial class ConversionInComparisonOperatorTest
{
    public void Foo()
    {
        decimal SomeDecimal = 12.3m;
        double ACalc = 32.1d;
        if (ACalc > (double)(60m / SomeDecimal))
        {
            Console.WriteLine(1);
        }
    }
}");
    }

    [Fact(Skip = "Too slow")]
    public async Task DeeplyNestedBinaryExpressionShouldNotStackOverflowAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class ConversionInComparisonOperatorTest
    Public Sub Foo()
       dim x = 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13 + 14 + 15 + 16 + 17 + 18 + 19 + 20 + 21 + 22 + 23 + 24 + 25 + 26 + 27 + 28 + 29 + 30 + 31 + 32 + 33 + 34 + 35 + 36 + 37 + 38 + 39 + 40 + 41 + 42 + 43 + 44 + 45 + 46 + 47 + 48 + 49 + 50 + 51 + 52 + 53 + 54 + 55 + 56 + 57 + 58 + 59 + 60 + 61 + 62 + 63 + 64 + 65 + 66 + 67 + 68 + 69 + 70 + 71 + 72 + 73 + 74 + 75 + 76 + 77 + 78 + 79 + 80 + 81 + 82 + 83 + 84 + 85 + 86 + 87 + 88 + 89 + 90 + 91 + 92 + 93 + 94 + 95 + 96 + 97 + 98 + 99 + 100 + 101 + 102 + 103 + 104 + 105 + 106 + 107 + 108 + 109 + 110 + 111 + 112 + 113 + 114 + 115 + 116 + 117 + 118 + 119 + 120 + 121 + 122 + 123 + 124 + 125 + 126 + 127 + 128 + 129 + 130 + 131 + 132 + 133 + 134 + 135 + 136 + 137 + 138 + 139 + 140 + 141 + 142 + 143 + 144 + 145 + 146 + 147 + 148 + 149 + 150 + 151 + 152 + 153 + 154 + 155 + 156 + 157 + 158 + 159 + 160 + 161 + 162 + 163 + 164 + 165 + 166 + 167 + 168 + 169 + 170 + 171 + 172 + 173 + 174 + 175 + 176 + 177 + 178 + 179 + 180 + 181 + 182 + 183 + 184 + 185 + 186 + 187 + 188 + 189 + 190 + 191 + 192 + 193 + 194 + 195 + 196 + 197 + 198 + 199 + 200 + 201 + 202 + 203 + 204 + 205 + 206 + 207 + 208 + 209 + 210 + 211 + 212 + 213 + 214 + 215 + 216 + 217 + 218 + 219 + 220 + 221 + 222 + 223 + 224 + 225 + 226 + 227 + 228 + 229 + 230 + 231 + 232 + 233 + 234 + 235 + 236 + 237 + 238 + 239 + 240 + 241 + 242 + 243 + 244 + 245 + 246 + 247 + 248 + 249 + 250 + 251 + 252 + 253 + 254 + 255 + 256 + 257 + 258 + 259 + 260 + 261 + 262 + 263 + 264 + 265 + 266 + 267 + 268 + 269 + 270 + 271 + 272 + 273 + 274 + 275 + 276 + 277 + 278 + 279 + 280 + 281 + 282 + 283 + 284 + 285 + 286 + 287 + 288 + 289 + 290 + 291 + 292 + 293 + 294 + 295 + 296 + 297 + 298 + 299 + 300 + 301 + 302 + 303 + 304 + 305 + 306 + 307 + 308 + 309 + 310 + 311 + 312 + 313 + 314 + 315 + 316 + 317 + 318 + 319 + 320 + 321 + 322 + 323 + 324 + 325 + 326 + 327 + 328 + 329 + 330 + 331 + 332 + 333 + 334 + 335 + 336 + 337 + 338 + 339 + 340 + 341 + 342 + 343 + 344 + 345 + 346 + 347 + 348 + 349 + 350 + 351 + 352 + 353 + 354 + 355 + 356 + 357 + 358 + 359 + 360 + 361 + 362 + 363 + 364 + 365 + 366 + 367 + 368 + 369 + 370 + 371 + 372 + 373 + 374 + 375 + 376 + 377 + 378 + 379 + 380 + 381 + 382 + 383 + 384 + 385 + 386 + 387 + 388 + 389 + 390 + 391 + 392 + 393 + 394 + 395 + 396 + 397 + 398 + 399 + 400 + 401 + 402 + 403 + 404 + 405 + 406 + 407 + 408 + 409 + 410 + 411 + 412 + 413 + 414 + 415 + 416 + 417 + 418 + 419 + 420 + 421 + 422 + 423 + 424 + 425 + 426 + 427 + 428 + 429 + 430 + 431 + 432 + 433 + 434 + 435 + 436 + 437 + 438 + 439 + 440 + 441 + 442 + 443 + 444 + 445 + 446 + 447 + 448 + 449 + 450 + 451 + 452 + 453 + 454 + 455 + 456 + 457 + 458 + 459 + 460 + 461 + 462 + 463 + 464 + 465 + 466 + 467 + 468 + 469 + 470 + 471 + 472 + 473 + 474 + 475 + 476 + 477 + 478 + 479 + 480 + 481 + 482 + 483 + 484 + 485 + 486 + 487 + 488 + 489 + 490 + 491 + 492 + 493 + 494 + 495 + 496 + 497 + 498 + 499 + 500 + 501 + 502 + 503 + 504 + 505 + 506 + 507 + 508 + 509 + 510 + 511 + 512 + 513 + 514 + 515 + 516 + 517 + 518 + 519 + 520 + 521 + 522 + 523 + 524 + 525 + 526 + 527 + 528 + 529 + 530 + 531 + 532 + 533 + 534 + 535 + 536 + 537 + 538 + 539 + 540 + 541 + 542 + 543 + 544 + 545 + 546 + 547 + 548 + 549 + 550 + 551 + 552 + 553 + 554 + 555 + 556 + 557 + 558 + 559 + 560 + 561 + 562 + 563 + 564 + 565 + 566 + 567 + 568 + 569 + 570 + 571 + 572 + 573 + 574 + 575 + 576 + 577 + 578 + 579 + 580 + 581 + 582 + 583 + 584 + 585 + 586 + 587 + 588 + 589 + 590 + 591 + 592 + 593 + 594 + 595 + 596 + 597 + 598 + 599 + 600 + 601 + 602 + 603 + 604 + 605 + 606 + 607 + 608 + 609 + 610 + 611 + 612 + 613 + 614 + 615 + 616 + 617 + 618 + 619 + 620 + 621 + 622 + 623 + 624 + 625 + 626 + 627 + 628 + 629 + 630 + 631 + 632 + 633 + 634 + 635 + 636 + 637 + 638 + 639 + 640 + 641 + 642 + 643 + 644 + 645 + 646 + 647 + 648 + 649 + 650 + 651 + 652 + 653 + 654 + 655 + 656 + 657 + 658 + 659 + 660 + 661 + 662 + 663 + 664 + 665 + 666 + 667 + 668 + 669 + 670 + 671 + 672 + 673 + 674 + 675 + 676 + 677 + 678 + 679 + 680 + 681 + 682 + 683 + 684 + 685 + 686 + 687 + 688 + 689 + 690 + 691 + 692 + 693 + 694 + 695 + 696 + 697 + 698 + 699 + 700 + 701 + 702 + 703 + 704 + 705 + 706 + 707 + 708 + 709 + 710 + 711 + 712 + 713 + 714 + 715 + 716 + 717 + 718 + 719 + 720 + 721 + 722 + 723 + 724 + 725 + 726 + 727 + 728 + 729 + 730 + 731 + 732 + 733 + 734 + 735 + 736 + 737 + 738 + 739 + 740 + 741 + 742 + 743 + 744 + 745 + 746 + 747 + 748 + 749 + 750 + 751 + 752 + 753 + 754 + 755 + 756 + 757 + 758 + 759 + 760 + 761 + 762 + 763 + 764 + 765 + 766 + 767 + 768 + 769 + 770 + 771 + 772 + 773 + 774 + 775 + 776 + 777 + 778 + 779 + 780 + 781 + 782 + 783 + 784 + 785 + 786 + 787 + 788 + 789 + 790 + 791 + 792 + 793 + 794 + 795 + 796 + 797 + 798 + 799 + 800 + 801 + 802 + 803 + 804 + 805 + 806 + 807 + 808 + 809 + 810 + 811 + 812 + 813 + 814 + 815 + 816 + 817 + 818 + 819 + 820 + 821 + 822 + 823 + 824 + 825 + 826 + 827 + 828 + 829 + 830 + 831 + 832 + 833 + 834 + 835 + 836 + 837 + 838 + 839 + 840 + 841 + 842 + 843 + 844 + 845 + 846 + 847 + 848 + 849 + 850 + 851 + 852 + 853 + 854 + 855 + 856 + 857 + 858 + 859 + 860 + 861 + 862 + 863 + 864 + 865 + 866 + 867 + 868 + 869 + 870 + 871 + 872 + 873 + 874 + 875 + 876 + 877 + 878 + 879 + 880 + 881 + 882 + 883 + 884 + 885 + 886 + 887 + 888 + 889 + 890 + 891 + 892 + 893 + 894 + 895 + 896 + 897 + 898 + 899 + 900 + 901 + 902 + 903 + 904 + 905 + 906 + 907 + 908 + 909 + 910 + 911 + 912 + 913 + 914 + 915 + 916 + 917 + 918 + 919 + 920 + 921 + 922 + 923 + 924 + 925 + 926 + 927 + 928 + 929 + 930 + 931 + 932 + 933 + 934 + 935 + 936 + 937 + 938 + 939 + 940 + 941 + 942 + 943 + 944 + 945 + 946 + 947 + 948 + 949 + 950 + 951 + 952 + 953 + 954 + 955 + 956 + 957 + 958 + 959 + 960 + 961 + 962 + 963 + 964 + 965 + 966 + 967 + 968 + 969 + 970 + 971 + 972 + 973 + 974 + 975 + 976 + 977 + 978 + 979 + 980 + 981 + 982 + 983 + 984 + 985 + 986 + 987 + 988 + 989 + 990 + 991 + 992 + 993 + 994 + 995 + 996 + 997 + 998 + 999 + 1000
    End Sub
End Class", @"
public partial class ConversionInComparisonOperatorTest
{
    public void Foo()
    {
        int x = 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13 + 14 + 15 + 16 + 17 + 18 + 19 + 20 + 21 + 22 + 23 + 24 + 25 + 26 + 27 + 28 + 29 + 30 + 31 + 32 + 33 + 34 + 35 + 36 + 37 + 38 + 39 + 40 + 41 + 42 + 43 + 44 + 45 + 46 + 47 + 48 + 49 + 50 + 51 + 52 + 53 + 54 + 55 + 56 + 57 + 58 + 59 + 60 + 61 + 62 + 63 + 64 + 65 + 66 + 67 + 68 + 69 + 70 + 71 + 72 + 73 + 74 + 75 + 76 + 77 + 78 + 79 + 80 + 81 + 82 + 83 + 84 + 85 + 86 + 87 + 88 + 89 + 90 + 91 + 92 + 93 + 94 + 95 + 96 + 97 + 98 + 99 + 100 + 101 + 102 + 103 + 104 + 105 + 106 + 107 + 108 + 109 + 110 + 111 + 112 + 113 + 114 + 115 + 116 + 117 + 118 + 119 + 120 + 121 + 122 + 123 + 124 + 125 + 126 + 127 + 128 + 129 + 130 + 131 + 132 + 133 + 134 + 135 + 136 + 137 + 138 + 139 + 140 + 141 + 142 + 143 + 144 + 145 + 146 + 147 + 148 + 149 + 150 + 151 + 152 + 153 + 154 + 155 + 156 + 157 + 158 + 159 + 160 + 161 + 162 + 163 + 164 + 165 + 166 + 167 + 168 + 169 + 170 + 171 + 172 + 173 + 174 + 175 + 176 + 177 + 178 + 179 + 180 + 181 + 182 + 183 + 184 + 185 + 186 + 187 + 188 + 189 + 190 + 191 + 192 + 193 + 194 + 195 + 196 + 197 + 198 + 199 + 200 + 201 + 202 + 203 + 204 + 205 + 206 + 207 + 208 + 209 + 210 + 211 + 212 + 213 + 214 + 215 + 216 + 217 + 218 + 219 + 220 + 221 + 222 + 223 + 224 + 225 + 226 + 227 + 228 + 229 + 230 + 231 + 232 + 233 + 234 + 235 + 236 + 237 + 238 + 239 + 240 + 241 + 242 + 243 + 244 + 245 + 246 + 247 + 248 + 249 + 250 + 251 + 252 + 253 + 254 + 255 + 256 + 257 + 258 + 259 + 260 + 261 + 262 + 263 + 264 + 265 + 266 + 267 + 268 + 269 + 270 + 271 + 272 + 273 + 274 + 275 + 276 + 277 + 278 + 279 + 280 + 281 + 282 + 283 + 284 + 285 + 286 + 287 + 288 + 289 + 290 + 291 + 292 + 293 + 294 + 295 + 296 + 297 + 298 + 299 + 300 + 301 + 302 + 303 + 304 + 305 + 306 + 307 + 308 + 309 + 310 + 311 + 312 + 313 + 314 + 315 + 316 + 317 + 318 + 319 + 320 + 321 + 322 + 323 + 324 + 325 + 326 + 327 + 328 + 329 + 330 + 331 + 332 + 333 + 334 + 335 + 336 + 337 + 338 + 339 + 340 + 341 + 342 + 343 + 344 + 345 + 346 + 347 + 348 + 349 + 350 + 351 + 352 + 353 + 354 + 355 + 356 + 357 + 358 + 359 + 360 + 361 + 362 + 363 + 364 + 365 + 366 + 367 + 368 + 369 + 370 + 371 + 372 + 373 + 374 + 375 + 376 + 377 + 378 + 379 + 380 + 381 + 382 + 383 + 384 + 385 + 386 + 387 + 388 + 389 + 390 + 391 + 392 + 393 + 394 + 395 + 396 + 397 + 398 + 399 + 400 + 401 + 402 + 403 + 404 + 405 + 406 + 407 + 408 + 409 + 410 + 411 + 412 + 413 + 414 + 415 + 416 + 417 + 418 + 419 + 420 + 421 + 422 + 423 + 424 + 425 + 426 + 427 + 428 + 429 + 430 + 431 + 432 + 433 + 434 + 435 + 436 + 437 + 438 + 439 + 440 + 441 + 442 + 443 + 444 + 445 + 446 + 447 + 448 + 449 + 450 + 451 + 452 + 453 + 454 + 455 + 456 + 457 + 458 + 459 + 460 + 461 + 462 + 463 + 464 + 465 + 466 + 467 + 468 + 469 + 470 + 471 + 472 + 473 + 474 + 475 + 476 + 477 + 478 + 479 + 480 + 481 + 482 + 483 + 484 + 485 + 486 + 487 + 488 + 489 + 490 + 491 + 492 + 493 + 494 + 495 + 496 + 497 + 498 + 499 + 500 + 501 + 502 + 503 + 504 + 505 + 506 + 507 + 508 + 509 + 510 + 511 + 512 + 513 + 514 + 515 + 516 + 517 + 518 + 519 + 520 + 521 + 522 + 523 + 524 + 525 + 526 + 527 + 528 + 529 + 530 + 531 + 532 + 533 + 534 + 535 + 536 + 537 + 538 + 539 + 540 + 541 + 542 + 543 + 544 + 545 + 546 + 547 + 548 + 549 + 550 + 551 + 552 + 553 + 554 + 555 + 556 + 557 + 558 + 559 + 560 + 561 + 562 + 563 + 564 + 565 + 566 + 567 + 568 + 569 + 570 + 571 + 572 + 573 + 574 + 575 + 576 + 577 + 578 + 579 + 580 + 581 + 582 + 583 + 584 + 585 + 586 + 587 + 588 + 589 + 590 + 591 + 592 + 593 + 594 + 595 + 596 + 597 + 598 + 599 + 600 + 601 + 602 + 603 + 604 + 605 + 606 + 607 + 608 + 609 + 610 + 611 + 612 + 613 + 614 + 615 + 616 + 617 + 618 + 619 + 620 + 621 + 622 + 623 + 624 + 625 + 626 + 627 + 628 + 629 + 630 + 631 + 632 + 633 + 634 + 635 + 636 + 637 + 638 + 639 + 640 + 641 + 642 + 643 + 644 + 645 + 646 + 647 + 648 + 649 + 650 + 651 + 652 + 653 + 654 + 655 + 656 + 657 + 658 + 659 + 660 + 661 + 662 + 663 + 664 + 665 + 666 + 667 + 668 + 669 + 670 + 671 + 672 + 673 + 674 + 675 + 676 + 677 + 678 + 679 + 680 + 681 + 682 + 683 + 684 + 685 + 686 + 687 + 688 + 689 + 690 + 691 + 692 + 693 + 694 + 695 + 696 + 697 + 698 + 699 + 700 + 701 + 702 + 703 + 704 + 705 + 706 + 707 + 708 + 709 + 710 + 711 + 712 + 713 + 714 + 715 + 716 + 717 + 718 + 719 + 720 + 721 + 722 + 723 + 724 + 725 + 726 + 727 + 728 + 729 + 730 + 731 + 732 + 733 + 734 + 735 + 736 + 737 + 738 + 739 + 740 + 741 + 742 + 743 + 744 + 745 + 746 + 747 + 748 + 749 + 750 + 751 + 752 + 753 + 754 + 755 + 756 + 757 + 758 + 759 + 760 + 761 + 762 + 763 + 764 + 765 + 766 + 767 + 768 + 769 + 770 + 771 + 772 + 773 + 774 + 775 + 776 + 777 + 778 + 779 + 780 + 781 + 782 + 783 + 784 + 785 + 786 + 787 + 788 + 789 + 790 + 791 + 792 + 793 + 794 + 795 + 796 + 797 + 798 + 799 + 800 + 801 + 802 + 803 + 804 + 805 + 806 + 807 + 808 + 809 + 810 + 811 + 812 + 813 + 814 + 815 + 816 + 817 + 818 + 819 + 820 + 821 + 822 + 823 + 824 + 825 + 826 + 827 + 828 + 829 + 830 + 831 + 832 + 833 + 834 + 835 + 836 + 837 + 838 + 839 + 840 + 841 + 842 + 843 + 844 + 845 + 846 + 847 + 848 + 849 + 850 + 851 + 852 + 853 + 854 + 855 + 856 + 857 + 858 + 859 + 860 + 861 + 862 + 863 + 864 + 865 + 866 + 867 + 868 + 869 + 870 + 871 + 872 + 873 + 874 + 875 + 876 + 877 + 878 + 879 + 880 + 881 + 882 + 883 + 884 + 885 + 886 + 887 + 888 + 889 + 890 + 891 + 892 + 893 + 894 + 895 + 896 + 897 + 898 + 899 + 900 + 901 + 902 + 903 + 904 + 905 + 906 + 907 + 908 + 909 + 910 + 911 + 912 + 913 + 914 + 915 + 916 + 917 + 918 + 919 + 920 + 921 + 922 + 923 + 924 + 925 + 926 + 927 + 928 + 929 + 930 + 931 + 932 + 933 + 934 + 935 + 936 + 937 + 938 + 939 + 940 + 941 + 942 + 943 + 944 + 945 + 946 + 947 + 948 + 949 + 950 + 951 + 952 + 953 + 954 + 955 + 956 + 957 + 958 + 959 + 960 + 961 + 962 + 963 + 964 + 965 + 966 + 967 + 968 + 969 + 970 + 971 + 972 + 973 + 974 + 975 + 976 + 977 + 978 + 979 + 980 + 981 + 982 + 983 + 984 + 985 + 986 + 987 + 988 + 989 + 990 + 991 + 992 + 993 + 994 + 995 + 996 + 997 + 998 + 999 + 1000;
    }
}");
    }
}