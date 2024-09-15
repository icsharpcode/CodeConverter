using Microsoft.VisualStudio.Extensibility.Settings;

namespace ICSharpCode.CodeConverter.VsExtension.ICSharpCode.CodeConverter.VsExtension;

public class ConverterSettingsAccessor
{
    private readonly SettingValues _settings;

    public ConverterSettingsAccessor(SettingValues settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public bool CopyResultToClipboardForSingleDocument => GetBooleanSetting(ConverterSettings.CopyResultToClipboardForSingleDocument);
    public bool AlwaysOverwriteFiles => GetBooleanSetting(ConverterSettings.AlwaysOverwriteFiles);
    public bool CreateBackups => GetBooleanSetting(ConverterSettings.CreateBackups);
    public int FormattingTimeout => GetIntegerSetting(ConverterSettings.FormattingTimeout);

    private bool GetBooleanSetting(Setting.Boolean setting)
    {
        return _settings.TryGetValue(setting.FullId, out var value) ? value.ValueOrDefault(setting.DefaultValue) : setting.DefaultValue;
    }

    private int GetIntegerSetting(Setting.Integer setting)
    {
        return _settings.TryGetValue(setting.FullId, out var value) ? value.ValueOrDefault(setting.DefaultValue) : setting.DefaultValue;
    }
}