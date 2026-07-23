using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Lumina.Data;
using PvPAnnouncer.Data;
using PvPAnnouncer.Impl;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _configuration;
    private readonly IShoutcastRepository _shoutcastRepository;


    public ConfigWindow(IShoutcastRepository shoutcastRepository, Configuration pluginConfiguration,
        IEventShoutcastMapping eventShoutcastMapping) : base(
        "NPCAnnouncer Configuration")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 425),
            MaximumSize = new Vector2(800, 1000)
        };

        SizeCondition = ImGuiCond.Always;

        _configuration = pluginConfiguration;
        _shoutcastRepository = shoutcastRepository;
    }

    public void Dispose()
    {
    }

    private void DoAttribute(string attr)
    {
        var attVar = _configuration.WantsAttribute(attr);
        if (ImGui.Checkbox(attr, ref attVar))
        {
            if (attVar)
            {
                _configuration.SetAttribute(attr);
            }
            else
            {
                _configuration.RemoveAttribute(attr);
            }

            _configuration.Save();
        }
    }


    public override void Draw()
    {
        var disabled = _configuration.Disabled;
        var muted = _configuration.Muted;
        var wolvesDen = _configuration.WolvesDen;
        var pvp = _configuration.PvP;
        var pve = _configuration.PvE;
        var pveCrits = _configuration.PvECrits;
        var overworld = _configuration.Overworld;

        var partyPvE = _configuration.PartyMembersPvE;
        var partyPvP = _configuration.PartyMembersPvP;

        var hideBattleText = _configuration.HideBattleText;
        var cooldown = _configuration.CooldownSeconds;
        var percent = _configuration.Percent;
        var repeatVoiceLine = _configuration.RepeatVoiceLineQueue;
        var repeatEventCommentary = _configuration.RepeatEventCommentaryQueue;
        var animationDelayFactor = _configuration.AnimationDelayFactor;
        var clearEventsAfter = _configuration.ClearEventsAfter;
        var clearVoicelinesAfter = _configuration.ClearVoicelinesAfter;
        var notify = _configuration.Notify;
        var icon = _configuration.WantsIcon;

        ImGui.TextWrapped("I love your feedback! It helps me make the plugin better! " +
                          "If you have any issues, suggestions, questions. I would love to hear them no matter how small!\n" +
                          "Use the plugin feedback button or contact .spider in the Dalamud Discord.");
        ImGui.Separator();

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

        if (ImGui.Checkbox("Disable Plugin", ref disabled))
        {
            _configuration.Disabled = disabled;
            _configuration.Save();
        }

        if (ImGui.Checkbox("Use Voice Lines in the Wolves Den", ref wolvesDen))
        {
            _configuration.WolvesDen = wolvesDen;
            _configuration.Save();
        }


        if (ImGui.Checkbox("Use Voice Lines in Overworld Areas", ref overworld))
        {
            _configuration.Overworld = overworld;
            _configuration.Save();
        }

        if (ImGui.Checkbox("Use Voice Lines in PvP Instances", ref pvp))
        {
            _configuration.PvP = pvp;
            _configuration.Save();
        }

        if (ImGui.Checkbox("Use Voice Lines in PvE Instances", ref pve))
        {
            _configuration.PvE = pve;
            _configuration.Save();
        }

        ImGui.Separator();
        ImGui.TextWrapped("If you wish to create new characters or manage existing voicelines and events click here:");
        if (ImGui.Button("Character, Event, and Voiceline Management###FirstButton"))
        {
            PluginServices.CustomizationWindow.Toggle();
        }


        ImGui.Text("Announcers: ");

        var c = 0;
        foreach (var caster in _shoutcastRepository.GetShoutcasters())
        {
            DoAttribute(caster);
            c++;
            if (c % 4 != 0)
            {
                ImGui.SameLine();
            }
        }

        ImGui.NewLine();

        ImGui.Separator();

        ImGui.TextWrapped("Use Voice Lines with the following attributes: ");
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "These two values allow announcers to use voice lines usually reserved for specific people. For example, \nMetem may say \"The Honey B. Lovely show has begun!\" if Honey B. Lovely is enabled.");
        var a = 0;
        foreach (var se in _shoutcastRepository.GetAttributes())
        {
            DoAttribute(se);
            a++;
            if (a % 4 != 0)
            {
                ImGui.SameLine();
            }
        }

        ImGui.NewLine();
        ImGui.Separator();

        if (ImGui.Checkbox(
                "Allow comments on critical hits during PvE (Warning, this may make announcers extra chatty depending on your settings)",
                ref pveCrits))
        {
            _configuration.PvECrits = pveCrits;
            _configuration.Save();
        }

        if (ImGui.Checkbox("Show Announcer Portrait", ref icon))
        {
            _configuration.WantsIcon = icon;
            _configuration.Save();
        }

        if (ImGui.Checkbox("Notify when Voice Volume is muted", ref notify))
        {
            _configuration.Notify = notify;
            _configuration.Save();
        }

        ImGui.Separator();

        if (ImGui.Checkbox("Announcers react to your party in PvP", ref partyPvP))
        {
            _configuration.PartyMembersPvP = partyPvP;
            _configuration.Save();
        }

        if (ImGui.Checkbox("Announcers react to your party in PvE", ref partyPvE))
        {
            _configuration.PartyMembersPvE = partyPvE;
            _configuration.Save();
        }

        ImGui.Separator();
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
            "Sometimes this plugin announces a split-second too early. This setting adds a very minor delay which should prevent announcements before an action finishes.");
        ImGui.Indent();

        if (ImGui.SliderInt("###SliderAnimationFactor", ref animationDelayFactor, 250, 2000, "%dms",
                ImGuiSliderFlags.AlwaysClamp))
        {
            _configuration.AnimationDelayFactor = animationDelayFactor;
            _configuration.Save();
        }

        ImGui.Unindent();


        ImGui.TextWrapped("Minimum unique voice lines to play before a repeated one may be played.");
        ImGui.Indent();
        if (ImGui.SliderInt("##SliderVoicelines", ref repeatVoiceLine, 1, 25))
        {
            _configuration.RepeatVoiceLineQueue = repeatVoiceLine;
            _configuration.Save();
        }

        ImGui.Unindent();


        ImGui.TextWrapped("Minimum number of events to announce before a repeated one is allowed to come up.");
        ImGui.Indent();
        if (ImGui.SliderInt("###SliderEvents", ref repeatEventCommentary, 1, 10))
        {
            _configuration.RepeatEventCommentaryQueue = repeatEventCommentary;
            _configuration.Save();
        }

        ImGui.Unindent();
        ImGui.TextWrapped("Maximum amount of minutes an event can be put on cooldown before being triggered");
        ImGui.Indent();
        if (ImGui.SliderInt("###SliderClearEvents", ref clearEventsAfter, 1, 10))
        {
            _configuration.ClearEventsAfter = clearEventsAfter;
            _configuration.Save();
        }

        ImGui.Unindent();

        ImGui.TextWrapped("Maximum amount of minutes a voiceline can be put on cooldown before being heard again");
        ImGui.Indent();
        if (ImGui.SliderInt("###SliderClearVoicelines", ref clearVoicelinesAfter, 1, 10))
        {
            _configuration.ClearVoicelinesAfter = clearVoicelinesAfter;
            _configuration.Save();
        }

        ImGui.Unindent();
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

        ImGui.Separator();
        ImGui.Text("Announcer Spoken Language:");
        DoLanguageVoiceSelection();

        ImGui.Text("Announcer Written Language:");
        ImGuiComponents.HelpMarker(
            "Due to how the plugin works, some voice lines do not have text equivalents in game. (specifically Mahjong Lines and Encrypted Voicelines from M12S). They have been manually transcribed to English. If you wish to help translate them to different languages, please contact the Plugin Developer.");

        DoLanguageTextSelection();
        ImGui.Separator();


        if (ImGui.Checkbox("Mute Announcer", ref muted))
        {
            _configuration.Muted = muted;
            _configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.Checkbox("Hide Battle Text", ref hideBattleText))
        {
            _configuration.HideBattleText = hideBattleText;
            _configuration.Save();
        }
    }

    private void DoLanguageVoiceSelection()
    {
        foreach (var keyValuePair in LanguageUtil.LanguageMap)
        {
            var k = keyValuePair.Key;
            var v = keyValuePair.Value;
            if (k == 0)
            {
                continue;
            }

            var keyName = Enum.GetName(k) ?? "Unknown Language";
            if (!PluginServices.DataManager.FileExists($"sound/voice/vo_line/8205353_{v}.scd"))
            {
                continue;
            }

            var lang = _configuration.Language;
            if (ImGui.RadioButton(keyName + "###" + "LanguageVoiceSelection" + v, lang.Equals(v)))
            {
                _configuration.Language = v;
                _configuration.Save();
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    private void DoLanguageTextSelection()
    {
        var langs = Enum.GetValues<ClientLanguage>().Cast<ClientLanguage>();

        foreach (var clientLangEnum in langs)
        {
            var enumCodeString = clientLangEnum.ToCode();

            var userFacingLang = Enum.GetName(clientLangEnum) ?? "Unknown Language";

            var configuredTextLang = _configuration.TextLanguage;

            if (ImGui.RadioButton(userFacingLang + "###" + "LanguageTextSelection" + enumCodeString,
                    configuredTextLang.Equals(enumCodeString)))
            {
                _configuration.TextLanguage = enumCodeString;
                _configuration.Save();
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }
}