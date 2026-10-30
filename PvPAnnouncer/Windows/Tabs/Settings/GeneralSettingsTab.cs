using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using OtterGui.Widgets;

namespace PvPAnnouncer.Windows.Tabs.Settings;

public class GeneralSettingsTab : ITab
{
    private readonly Configuration _configuration = PluginServices.Config;
    private readonly TagButtons _restriction = new();

    private static readonly uint[] ContentTypes =
    [
        2, 3, 4, 5, 7, 9, 16, 19, 21, 26, 27, 28, 29, 30, 37, 38, 40
    ];

    public ReadOnlySpan<byte> Label => "General"u8;

    public void DrawContent()
    {
        DrawMainToggles();
    }

    private void DrawMainToggles()
    {
        ImGui.TextWrapped("Where should announcers be heard?");
        ImGui.Separator();
        DrawYap();
        ImGui.NewLine();

        ImGui.TextWrapped("Should Announcers be disabled anywhere within PVE Instances?");
        ImGui.Separator();
        DrawContentRestrictionSettings();
        ImGui.NewLine();

        ImGui.TextWrapped("How Should Duty Support be handled?");
        ImGui.Separator();
        DrawDutySupportSettings();
        ImGui.NewLine();

        ImGui.TextWrapped("Additional Behavior");
        ImGui.Separator();
        DrawSpecialRestrictions();
        ImGui.NewLine();

        ImGui.TextWrapped("Should Announcers react to other players?");
        ImGui.Separator();
        DrawReact();
        ImGui.NewLine();

        ImGui.TextWrapped("How should these announcements be displayed?");
        ImGui.Separator();
        DrawDisplay();
    }

    private void DrawYap()
    {
        var pvp = _configuration.PvP;
        var pve = _configuration.PvE;
        var overworld = _configuration.Overworld;
        if (ImGui.Checkbox("During PvP", ref pvp))
        {
            _configuration.PvP = pvp;
            _configuration.Save();
        }

        if (ImGui.Checkbox("During PvE", ref pve))
        {
            _configuration.PvE = pve;
            _configuration.Save();
        }

        if (ImGui.Checkbox("In Overworld Areas", ref overworld))
        {
            _configuration.Overworld = overworld;
            _configuration.Save();
        }
    }

    private void DrawReact()
    {
        var partyPvE = _configuration.PartyMembersPvE;
        var partyPvP = _configuration.PartyMembersPvP;

        if (ImGui.Checkbox("React to my party in PvP", ref partyPvP))
        {
            _configuration.PartyMembersPvP = partyPvP;
            _configuration.Save();
        }

        if (ImGui.Checkbox("React to my party in PvE", ref partyPvE))
        {
            _configuration.PartyMembersPvE = partyPvE;
            _configuration.Save();
        }
    }

    private void DrawDisplay()
    {
        var hideBattleText = _configuration.HideBattleText;
        var notify = _configuration.Notify;
        var icon = _configuration.WantsIcon;
        var pveCrits = _configuration.PvECrits;
        if (ImGui.Checkbox(
                "Allow comments on critical hits during PvE",
                ref pveCrits))
        {
            _configuration.PvECrits = pveCrits;
            _configuration.Save();
        }

        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Warning, this may make announcers extra chatty depending on your settings");
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

        if (ImGui.Checkbox("Hide Battle Text", ref hideBattleText))
        {
            _configuration.HideBattleText = hideBattleText;
            _configuration.Save();
        }
    }

    private void DrawSpecialRestrictions()
    {
        var list = PluginServices.RestrictionRepository.GetShoutcasterRestrictions();
        foreach (var shoutcasterRestriction in list)
        {
            var disabled = _configuration.Restrictions.Contains(shoutcasterRestriction.id);
            if (ImGui.Checkbox($"{shoutcasterRestriction.restrictionName}", ref disabled))
            {
                if (disabled)
                {
                    if (!_configuration.Restrictions.Contains(shoutcasterRestriction.id))
                    {
                        _configuration.Restrictions.Add(shoutcasterRestriction.id);
                        _configuration.Save();
                    }
                }
                else
                {
                    _configuration.Restrictions.Remove(shoutcasterRestriction.id);
                }
            }
        }
    }

    private void DrawContentRestrictionSettings()
    {
        if (!_configuration.PvE)
        {
            ImGui.TextWrapped("PVE Duties are disabled. Announcers will not play anywhere in PVE.");
            return;
        }

        var names = ContentTypes.Where(_configuration.DisabledContent.Contains).Select(GetContentTypeName).ToList();
        ImGui.NewLine();
        _restriction.Draw("Disabled:", "", names, out _, false);
        if (ImGui.BeginCombo("###Disabled Content", $"{names.Count} areas disabled"))
        {
            foreach (var contentType in ContentTypes)
            {
                var disabled = _configuration.DisabledContent.Contains(contentType);
                if (ImGui.Checkbox(GetContentTypeName(contentType), ref disabled))
                {
                    if (disabled)
                        _configuration.DisabledContent.Add(contentType);
                    else
                        _configuration.DisabledContent.Remove(contentType);

                    _configuration.Save();
                }
            }

            ImGui.EndCombo();
        }
    }

    private static string GetContentTypeName(uint id)
    {
        var name = id switch
        {
            2 => "Dungeons",
            3 => "Guildhests",
            4 => "Trials",
            5 => "Raids",
            7 => "Quest Battles",
            9 => "Treasure Dungeons",
            16 => "Ocean Fishing and the Diadem",
            19 => "Gold Saucer",
            21 => "Deep Dungeons",
            26 => "Eureka",
            27 => "Masked Carnivale",
            28 => "Ultimate Raids",
            29 => "Save the Queen",
            30 => "Variant and Criterion Dungeons",
            37 => "Chaotic Alliance Raids",
            38 => "Occult Crescent",
            40 => "Beastmaster Duties",
            _ => id.ToString()
        };

        return name;
    }

    private void DrawDutySupportSettings()
    {
        if (!_configuration.PvE)
        {
            ImGui.TextWrapped("PVE Duties are disabled. Announcers will not play anywhere in PVE.");
            return;
        }

        var wantsTrust = _configuration.WantsTrustNpc;
        var reallyWantsTrustNpc = _configuration.ReallyWantsTrustNpc;
        var onlyWantsTrustNpc = _configuration.OnlyWantsTrustNpc;

        if (ImGui.Checkbox("Play Voicelines from Trust NPC's while in Duty Support", ref wantsTrust))
        {
            _configuration.WantsTrustNpc = wantsTrust;
            _configuration.Save();
        }

        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "NPCAnnouncer will still play voicelines from characters in your party when in Trust/Duty Support.");

        if (wantsTrust)
        {
            if (ImGui.Checkbox("Play Trust voicelines regardless of Selected Announcers", ref reallyWantsTrustNpc))
            {
                _configuration.ReallyWantsTrustNpc = reallyWantsTrustNpc;
                _configuration.Save();
            }

            ImGui.SameLine();
            ImGuiComponents.HelpMarker(
                "NPCAnnouncer will play trust voicelines even when they aren't explicitly selected.");

            if (ImGui.Checkbox("Only Play Voicelines of Trusts that have been selected while in Duty Support.",
                    ref onlyWantsTrustNpc))
            {
                _configuration.OnlyWantsTrustNpc = onlyWantsTrustNpc;
                _configuration.Save();
            }

            ImGui.SameLine();
            ImGuiComponents.HelpMarker(
                "You must still pick announcers for this to work. If you want all trusts, select the above toggle as well.");
        }
    }
}