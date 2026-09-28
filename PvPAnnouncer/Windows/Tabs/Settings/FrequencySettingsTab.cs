using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using OtterGui.Widgets;
using PvPAnnouncer.Data;

namespace PvPAnnouncer.Windows.Tabs.Settings;

public class FrequencySettingsTab : ITab
{
    private readonly Configuration _configuration = PluginServices.Config;


    public ReadOnlySpan<byte> Label => "Frequency"u8;

    public void DrawContent()
    {
        DrawFrequency();
    }

    private void DrawFrequency()
    {
        var cooldown = _configuration.CooldownSeconds;
        var percent = _configuration.Percent;
        var repeatVoiceLine = _configuration.RepeatVoiceLineQueue;
        var repeatEventCommentary = _configuration.RepeatEventCommentaryQueue;
        var animationDelayFactor = _configuration.AnimationDelayFactor;
        var clearEventsAfter = _configuration.ClearEventsAfter;
        var clearVoicelinesAfter = _configuration.ClearVoicelinesAfter;
        ImGui.TextWrapped("Minimum delay between announcements");
        ImGui.Indent();
        if (ImGui.SliderInt("###SliderCooldown", ref cooldown, 1, 120, "%ds", ImGuiSliderFlags.AlwaysClamp))
        {
            _configuration.CooldownSeconds = cooldown;
            _configuration.Save();
        }

        ImGui.Unindent();


        ImGui.TextWrapped("Announcement Frequency");
        ImGuiComponents.HelpMarker("This controls the chance of announcing any given event.");
        ImGui.Indent();

        if (ImGui.SliderInt("###SliderPercent", ref percent, 1, 100, "%d%%", ImGuiSliderFlags.AlwaysClamp))
        {
            _configuration.Percent = percent;
            _configuration.Save();
        }

        ImGui.Unindent();

        ImGui.TextWrapped("Announcement Delay");
        ImGuiComponents.HelpMarker(
            "Sometimes this plugin triggers a split-second too early. This setting adds a very minor delay which should prevent announcements before an action finishes.");
        ImGui.Indent();

        if (ImGui.SliderInt("###SliderAnimationFactor", ref animationDelayFactor, 250, 2000, "%dms",
                ImGuiSliderFlags.AlwaysClamp))
        {
            _configuration.AnimationDelayFactor = animationDelayFactor;
            _configuration.Save();
        }

        ImGui.Unindent();


        ImGui.TextWrapped("Unique Lines Before a repeat");
        ImGui.Indent();
        if (ImGui.SliderInt("##SliderVoicelines", ref repeatVoiceLine, 1, 25))
        {
            _configuration.RepeatVoiceLineQueue = repeatVoiceLine;
            _configuration.Save();
        }

        ImGui.Unindent();


        ImGui.TextWrapped("Unique Events Before a repeat");
        ImGui.Indent();
        if (ImGui.SliderInt("###SliderEvents", ref repeatEventCommentary, 1, 10))
        {
            _configuration.RepeatEventCommentaryQueue = repeatEventCommentary;
            _configuration.Save();
        }

        ImGui.Unindent();
        ImGui.TextWrapped("Event Repeat Timeout");
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "Even if other events haven't occured yet, a repeat can be heard after this many minutes.");
        ImGui.Indent();
        if (ImGui.SliderInt("###SliderClearEvents", ref clearEventsAfter, 1, 10))
        {
            _configuration.ClearEventsAfter = clearEventsAfter;
            _configuration.Save();
        }

        ImGui.Unindent();

        ImGui.TextWrapped("Voiceline Repeat Timeout");
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "Even if other voice lines haven't played yet, a repeat can be heard after this many minutes.");
        ImGui.Indent();
        if (ImGui.SliderInt("###SliderClearVoicelines", ref clearVoicelinesAfter, 1, 10))
        {
            _configuration.ClearVoicelinesAfter = clearVoicelinesAfter;
            _configuration.Save();
        }

        ImGui.Unindent();
        ImGui.Separator();
        if (ImguiTools.CtrlShiftButton("Reset Above Values to Default"))
        {
            _configuration.CooldownSeconds = 15;
            _configuration.Percent = 70;
            _configuration.RepeatVoiceLineQueue = 3;
            _configuration.RepeatEventCommentaryQueue = 3;
            _configuration.AnimationDelayFactor = 250;
            _configuration.ClearVoicelinesAfter = 5;
            _configuration.ClearEventsAfter = 2;
            _configuration.Save();
        }
    }
}