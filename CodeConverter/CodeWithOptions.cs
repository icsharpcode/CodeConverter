namespace ICSharpCode.CodeConverter;

public class CodeWithOptions
{
    private static readonly HashSet<Type> _typesToFindAssemblyReferencesFrom = new() {
        typeof(System.Text.Encoding),
        typeof(System.ComponentModel.DefaultValueAttribute),
        typeof(System.Dynamic.DynamicObject),
        typeof(System.Net.Http.HttpClient),
        typeof(System.Xml.XmlConvert),
        typeof(System.Xml.Linq.XElement),
        typeof(Microsoft.VisualBasic.Constants)};

    private static IReadOnlyCollection<PortableExecutableReference> NetStandard3MetadataReferences => GetRefs(_typesToFindAssemblyReferencesFrom).ToArray();

    private static IEnumerable<PortableExecutableReference> GetRefs(IReadOnlyCollection<Type> types)
    {
        return types.Select(type => MetadataReference.CreateFromFile(type.Assembly.Location));
    }

    public string Text { get; private set; }
    public string FromLanguage { get; private set; }
    [Obsolete("This option has no effect. Please use a code formatter to format the output code.")]
    public int FromLanguageVersion { get; private set; }
    public string ToLanguage { get; private set; }
    [Obsolete("This option has no effect. Please use a code formatter to format the output code.")]
    public int ToLanguageVersion { get; private set; }

    public IReadOnlyCollection<PortableExecutableReference> References { get; set; } = new List<PortableExecutableReference>();

    public CodeWithOptions(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        FromLanguage = LanguageNames.CSharp;
        ToLanguage = LanguageNames.VisualBasic;
    }

    public CodeWithOptions SetFromLanguage(string name = LanguageNames.CSharp)
    {
        FromLanguage = name;
        return this;
    }

    public CodeWithOptions SetToLanguage(string name = LanguageNames.VisualBasic)
    {
        ToLanguage = name;
        return this;
    }

    [Obsolete("The version option has no effect, the latest version will be used.")]
    public CodeWithOptions SetFromLanguage(string name, int version)
    {
        FromLanguage = name;
        return this;
    }

    [Obsolete("The version option has no effect, the latest version will be used. Please use a code formatter to format the output code.")]
    public CodeWithOptions SetToLanguage(string name, int version)
    {
        ToLanguage = name;
        return this;
    }

    public CodeWithOptions WithTypeReferences(IReadOnlyCollection<PortableExecutableReference> references = null)
    {
        References = references ?? NetStandard3MetadataReferences;
        return this;
    }
}