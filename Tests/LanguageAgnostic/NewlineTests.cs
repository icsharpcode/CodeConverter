using ICSharpCode.CodeConverter.Util;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.LanguageAgnostic;

public class NewlineTests
{
    [Fact] public void NewlineReplacedWithCharacter() => Assert.Equal("'  '", "'\r\n\r\n'".WithoutNewLines());
    [Fact] public void CarriageReturnReplacedWithCharacter() => Assert.Equal("'  '", "'\r\r'".WithoutNewLines());
    [Fact] public void LineFeedReplacedWithCharacter() => Assert.Equal("'  '", "'\n\n'".WithoutNewLines());
}
