using System;
using Dalamud.Bindings.ImGui;
using OtterGui.Widgets;
using PvPAnnouncer.Data;

namespace PvPAnnouncer.Windows.Tabs.Voicelines;

public class CreateTab : ITab
{
    public ReadOnlySpan<byte> Label => "Create"u8;

    public void DrawContent()
    {
        ImGui.TextWrapped(
            "- In order for the plugin to play a voiceline, it needs an audio file and a text transcription.");
        ImGui.TextWrapped(
            "- While some voice line audio is transcribed neatly, most audio is independent from its transcription.");
        ImGui.TextWrapped(
            "- We must connect the dots ourselves in order to create a full shoutcast that the plugin can use.");

        ImGui.TextWrapped("Shoutcast creation can be done here:");
        if (ImGui.Button("Create Shoutcasts")) PluginServices.VoicelineCreationWindow.Toggle();

        ImGui.TextWrapped(
            "Once we have created a few shoutcasts, we must \"map\" the shoutcast to an associated event. This allows the plugin to select it when an event is triggered." +
            "\nThis can be done in the Assign tab.");

        ImGui.Separator();
        if (ImguiTools.CtrlShiftButton("Reset Custom Voicelines"))
        {
            PluginServices.Config.CustomShoutcasts.Clear();
            PluginServices.Config.Save();
            PluginServices.ConfigManager.ReloadConfig();
            PluginServices.ChatGui.Print("Reset Custom Voicelines!");
        }
    }
}