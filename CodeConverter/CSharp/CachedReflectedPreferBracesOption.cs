using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.CodeAnalysis.Options;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// The C# code style option "Prefer braces" (csharp_prefer_braces), which Roslyn keeps internal:
/// https://github.com/dotnet/roslyn/blob/main/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/CodeStyle/CSharpCodeStyleOptions.cs
/// </summary>
/// <remarks>
/// Use with the same care as <see cref="CachedReflectedDelegates"/>: an internal rename in another Roslyn version breaks this,
/// so each member returns null rather than throwing, and callers must have a fallback.
/// </remarks>
internal static class CachedReflectedPreferBracesOption
{
    private static readonly Lazy<IOption> PreferBracesOption = new(FindPreferBracesOption);

    /// <summary>
    /// The option value for each preference, or null where it can't be built or doesn't stick when set
    /// </summary>
    private static readonly ConcurrentDictionary<string, object> VerifiedValueByPreference = new();

    /// <param name="preference">The name of a Roslyn PreferBracesPreference: None, WhenMultiline or Always</param>
    /// <returns>The options with "Prefer braces" set to <paramref name="preference"/>, or null if it can't be set (checked once by reading it back)</returns>
    public static OptionSet WithPreferBraces(this OptionSet options, string preference)
    {
        if (PreferBracesOption.Value is not { } option) return null;
        var value = VerifiedValueByPreference.GetOrAdd(preference, p => CreateVerifiedValue(option, options, p));
        return value == null ? null : WithChangedOptionOrNull(options, option, value);
    }

    private static object CreateVerifiedValue(IOption option, OptionSet options, string preference)
    {
        try {
            var value = CreateCodeStyleOption(option.Type, preference);
            return WithChangedOptionOrNull(options, option, value)?.GetPreferBraces() == preference ? value : null;
        } catch (Exception) {
            return null;
        }
    }

    private static OptionSet WithChangedOptionOrNull(OptionSet options, IOption option, object value)
    {
        try {
            return options.WithChangedOption(new OptionKey(option), value);
        } catch (Exception) {
            return null;
        }
    }

    /// <returns>The name of the "Prefer braces" PreferBracesPreference in <paramref name="options"/>, or null if it can't be read</returns>
    public static string GetPreferBraces(this OptionSet options)
    {
        try {
            if (PreferBracesOption.Value is not { } option) return null;
            var codeStyleOption = options.GetOption(new OptionKey(option));
            return codeStyleOption?.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(codeStyleOption)?.ToString();
        } catch (Exception) {
            return null;
        }
    }

    private static IOption FindPreferBracesOption()
    {
        try {
            return typeof(CS.Formatting.CSharpFormattingOptions).Assembly
                .GetType("Microsoft.CodeAnalysis.CSharp.CodeStyle.CSharpCodeStyleOptions")
                ?.GetField("PreferBraces", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null) as IOption;
        } catch (Exception) {
            return null;
        }
    }

    /// <summary>
    /// Creates <c>new CodeStyleOption2&lt;PreferBracesPreference&gt;(preference, NotificationOption2.Silent)</c>
    /// </summary>
    private static object CreateCodeStyleOption(Type codeStyleOptionType, string preference)
    {
        var constructor = codeStyleOptionType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(c => c.GetParameters().Length == 2);
        var notificationType = constructor.GetParameters()[1].ParameterType;
        var silent = notificationType.GetProperty("Silent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                     ?? notificationType.GetField("Silent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        var preferenceValue = Enum.Parse(codeStyleOptionType.GetGenericArguments()[0], preference);
        return constructor.Invoke(new[] { preferenceValue, silent });
    }
}
