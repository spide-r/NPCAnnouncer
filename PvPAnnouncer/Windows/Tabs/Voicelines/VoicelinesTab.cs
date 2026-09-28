using System;
using Dalamud.Bindings.ImGui;
using OtterGui.Widgets;
using PvPAnnouncer.Windows.Tabs.Events;

namespace PvPAnnouncer.Windows.Tabs.Voicelines;

public class VoicelinesTab : ITab
{
    private readonly Configuration _configuration = PluginServices.Config;

    public ReadOnlySpan<byte> Label
        => "Voicelines"u8;

    private readonly ITab[] _tabs = [new LibraryTab(), new CreateTab(), new AssignTab(), new TranslateTab()];


    public void DrawContent()
    {
        TabBar.Draw("Voicelines Tab##VoicelinesTab", ImGuiTabBarFlags.None, _tabs);
    }
}