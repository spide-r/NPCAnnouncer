using System;
using Dalamud.Bindings.ImGui;
using OtterGui.Widgets;

namespace PvPAnnouncer.Windows.Tabs.Settings;

public class SettingsTab : ITab
{
    private readonly Configuration _configuration = PluginServices.Config;

    public ReadOnlySpan<byte> Label
        => "Settings"u8;

    private readonly ITab[] _tabs =
        [new GeneralSettingsTab(), new AnnouncerSettingsTab(), new FrequencySettingsTab(), new LanguageSettingsTab()];


    public void DrawContent()
    {
        TabBar.Draw("Settings Tab##SettingsTab", ImGuiTabBarFlags.None, _tabs);
    }
}