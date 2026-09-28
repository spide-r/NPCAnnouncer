using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using OtterGui.Widgets;
using PvPAnnouncer.Data;
using PvPAnnouncer.Impl;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Windows.Tabs.Settings;

public class AnnouncerSettingsTab : ITab
{
    private readonly Configuration _configuration = PluginServices.Config;
    private readonly IShoutcastRepository _shoutcastRepository = PluginServices.ShoutcastRepository;

    public ReadOnlySpan<byte> Label => "Announcer"u8;

    public void DrawContent()
    {
        DrawAnnouncerToggles();
    }

    private void DrawAnnouncerToggles()
    {
        TestAnnouncerButton();
        ImGui.TextWrapped("Who Should React to your gameplay?");
        ImGui.Separator();
        DrawAnnouncerTogglesList();
        ImGui.NewLine();
        ImGui.NewLine();

        ImGui.TextWrapped("Use Voice Lines with the following attributes: ");
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "These values allow announcers to use voice lines usually reserved for specific people. For example, \nMetem may say \"The Honey B. Lovely show has begun!\" if Honey B. Lovely is enabled.");
        ImGui.Separator();

        DrawAttributeTogglesList();
    }

    private void DrawAnnouncerTogglesList()
    {
        var c = 0;
        foreach (var caster in _shoutcastRepository.GetShoutcasters())
        {
            DoAttribute(caster);
            c++;
            if (c % 4 != 0) ImGui.SameLine();
        }
    }

    private void DrawAttributeTogglesList()
    {
        var a = 0;
        foreach (var se in _shoutcastRepository.GetAttributes())
        {
            DoAttribute(se);
            a++;
            if (a % 4 != 0) ImGui.SameLine();
        }
    }


    private void TestAnnouncerButton()
    {
        if (ImGui.Button("Test Current Announcer Selection"))
        {
            PluginServices.PlayerStateTracker.CheckSoundState();
            var bt = _shoutcastRepository.GetShoutcasts()
                .Where(bt => PluginServices.Config.WantsAttribute(bt.Shoutcaster))
                .Where(bt => PluginServices.Config.WantsAllAttributes(bt.Attributes)).ToArray();
            if (bt.Length != 0) // no announcers selected
            {
                var e = bt[Random.Shared.Next(bt.Length)];
                PluginServices.Announcer.PlayAndSendBattleTalkForTesting(e);
                PluginServices.ChatGui.Print($"Playing Voiceline for {e.Shoutcaster}", InternalConstants.MessageTag);
            }
            else
            {
                var dict = new Dictionary<string, string>
                {
                    ["en"] = "You don't have any announcers selected!"
                };
                var s = new ShoutcastBuilder(PluginServices.DataManager)
                    .WithSoundPath(InternalConstants.DefaultSoundPath)
                    .WithId("OopsAnnouncerDev").WithShoutcaster(InternalConstants.PvPAnnouncerDevName)
                    .WithIcon(InternalConstants.PvPAnnouncerDevIcon)
                    .WithTranscription(dict).BuildAndRefreshProperties();
                PluginServices.Announcer.SendBattleTalk(s);
            }
        }
    }

    private void DoAttribute(string attr)
    {
        var attVar = _configuration.WantsAttribute(attr);
        if (ImGui.Checkbox(attr, ref attVar))
        {
            if (attVar)
                _configuration.SetAttribute(attr);
            else
                _configuration.RemoveAttribute(attr);

            _configuration.Save();
        }

        if (attr.Equals(InternalConstants.MahjongAttribute))
            ImGuiComponents.HelpMarker(
                "If you use a language other than english, this determines if you want to hear voicelines that mention mahjong terms." +
                "\nThis has no effect on English voicelines");
    }
}