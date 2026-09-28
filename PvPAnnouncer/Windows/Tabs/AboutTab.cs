using System;
using Dalamud.Bindings.ImGui;
using OtterGui.Widgets;

namespace PvPAnnouncer.Windows.Tabs;

public class AboutTab : ITab
{
    public ReadOnlySpan<byte> Label => "About"u8;

    public void DrawContent()
    {
        ImGui.TextWrapped(
            "Welcome to NPC Announcer! This plugin will take many NPC's and put them into your game! " +
            "\nPlease contact .spider in the Dalamud Discord for feedback/suggestions!");
        ImGui.Spacing();
        ImGui.Text("Attributions");
        ImGui.BulletText("DeathRecap, VFXEditor, OofPlugin");
        ImGui.BulletText("Mutant Standard for the plugin icon (CC BY-NC-SA) - https://mutant.tech");
    }
}