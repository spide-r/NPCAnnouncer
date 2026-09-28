using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using OtterGui.Widgets;
using PvPAnnouncer.Data;
using PvPAnnouncer.Impl;
using PvPAnnouncer.Interfaces;
using PvPAnnouncer.Windows.Tabs;
using PvPAnnouncer.Windows.Tabs.Events;
using PvPAnnouncer.Windows.Tabs.Settings;
using PvPAnnouncer.Windows.Tabs.Voicelines;

namespace PvPAnnouncer.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _configuration;
    private readonly IShoutcastRepository _shoutcastRepository;


    public ConfigWindow(IShoutcastRepository shoutcastRepository, Configuration pluginConfiguration) : base(
        "NPCAnnouncer Configuration")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 425)
        };

        SizeCondition = ImGuiCond.Always;

        _configuration = pluginConfiguration;
        _shoutcastRepository = shoutcastRepository;
    }

    public void Dispose()
    {
    }

    private readonly ITab[] _tabs =
        [new SettingsTab(), new VoicelinesTab(), new EventsTab(), new ShareTab(), new AboutTab()];

    public override void Draw()
    {
        DrawHeader();
        TabBar.Draw("NPC Announcer Tab##AnnouncerTab", ImGuiTabBarFlags.None, _tabs);
    }

    private void DrawHeader()
    {
        var disabled = PluginServices.Config.Disabled;
        var muted = PluginServices.Config.Muted;
        if (ImGui.Button("Test The Announcer"))
        {
            PluginServices.PlayerStateTracker.CheckSoundState();
            Shoutcast[] bt = _shoutcastRepository.GetShoutcasts()
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

        ImGui.SameLine();
        if (ImGui.Checkbox("Disable Plugin", ref disabled))
        {
            _configuration.Disabled = disabled;
            _configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.Checkbox("Mute Announcer", ref muted))
        {
            _configuration.Muted = muted;
            _configuration.Save();
        }
    }
}