using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using OtterGui.Widgets;
using PvPAnnouncer.Data;

namespace PvPAnnouncer.Windows.Tabs.Events;

public class AssignTab : ITab
{
    private readonly Configuration _configuration = PluginServices.Config;
    private int _pvpEventSelection;
    private int _currentEventShoutSelection;
    private int _otherShouts;
    private int _shoutcasterSelection;
    public ReadOnlySpan<byte> Label => "Assign"u8;

    public void DrawContent()
    {
        var eventsUserFacingName = new List<string>();
        var eventsInternal = new List<string>();
        foreach (var e in PluginServices.PvPEventBroker.GetPvPEvents())
        {
            var eventId = e.Id;

            eventsUserFacingName.Add(e.Name);
            eventsInternal.Add(eventId);
        }

        ImGui.Text("Event Selector:");
        ImGui.Text("Character Filter");
        var shoutcasterSelection = _shoutcasterSelection;
        var shoutcasters = PluginServices.ShoutcastRepository.GetShoutcasters().ToList();

        if (ImGui.Combo("###ShoutcasterFilter", ref shoutcasterSelection, shoutcasters))
            _shoutcasterSelection = shoutcasterSelection;

        var selection = "";
        try
        {
            selection = shoutcasters[shoutcasterSelection];
        }
        catch (ArgumentOutOfRangeException)
        {
            if (shoutcasters.Count > 0)
                shoutcasterSelection = 0;
            else
                ImGui.Text("Error! Something is wrong and you have no shoutcasters available! Contact the developer!");
        }

        ImGui.Separator();
        var pvpEventSelection = _pvpEventSelection;

        if (ImGui.Combo("###PvPEvents", ref pvpEventSelection, eventsUserFacingName))
            _pvpEventSelection = pvpEventSelection;


        var currentShoutMapping =
            PluginServices.EventShoutcastMapping.GetShoutcastList(eventsInternal[pvpEventSelection]);

        var currentShoutsForEvent = PluginServices.ShoutcastRepository.GetShoutcasts()
            .Where(s => currentShoutMapping.Contains(s.Id))
            .Where(s => s.Shoutcaster.Equals(shoutcasters[shoutcasterSelection])).Select(e => e.Id).ToList();

        ImGui.Text("Current Voicelines:");
        if (currentShoutsForEvent.Count == 0)
            ImGui.TextWrapped(
                $"No Voicelines for {shoutcasters[shoutcasterSelection]} are present in this event. Add one?");

        var currentShoutSelection = _currentEventShoutSelection;
        if (ImGui.ListBox("###CurrentShouts", ref currentShoutSelection, currentShoutsForEvent))
            _currentEventShoutSelection = currentShoutSelection;

        if (ImGui.Button("Test Selected Shout###CurrentShoutTest"))
            try
            {
                var sc = PluginServices.ShoutcastRepository.GetShoutcast(currentShoutsForEvent[currentShoutSelection]);
                if (sc == null)
                {
                    PluginServices.PluginLog.Warning($"{currentShoutsForEvent[currentShoutSelection]} not found");
                    return;
                }

                PluginServices.Announcer.PlayAndSendBattleTalkForTesting(sc);
            }
            catch (ArgumentOutOfRangeException)
            {
            }

        ImGui.SameLine();
        if (ImGui.Button("Remove Voiceline"))
            try
            {
                var shoutToRemove = currentShoutsForEvent[currentShoutSelection];
                var eventToRemove = eventsInternal[pvpEventSelection];
                PluginServices.EventShoutcastMapping.RemoveShoutcast(eventToRemove, shoutToRemove);
                SaveAndRemove(eventToRemove, shoutToRemove);
            }
            catch (ArgumentOutOfRangeException)
            {
            }

        ImGui.Text("Available Voicelines:");

        var otherShoutsList = PluginServices.ShoutcastRepository.GetShoutcasts()
            .Where(s => !currentShoutsForEvent.Contains(s.Id))
            .Where(s => s.Shoutcaster.Equals(shoutcasters[shoutcasterSelection])).Select(e => e.Id).ToList();
        var otherShoutsSelection = _otherShouts;

        if (ImGui.ListBox("###OtherShouts", ref otherShoutsSelection, otherShoutsList))
            _otherShouts = otherShoutsSelection;

        if (ImGui.Button("Test Selected Shout###OtherShoutTest"))
            try
            {
                var sc = PluginServices.ShoutcastRepository.GetShoutcast(otherShoutsList[otherShoutsSelection]);
                if (sc == null)
                {
                    PluginServices.PluginLog.Warning($"{otherShoutsList[currentShoutSelection]} not found");
                    return;
                }

                PluginServices.Announcer.PlayAndSendBattleTalkForTesting(sc);
            }
            catch (ArgumentOutOfRangeException)
            {
            }

        ImGui.SameLine();
        if (ImGui.Button("Add Voiceline"))
            try
            {
                var shoutToAdd = otherShoutsList[otherShoutsSelection];
                var eventToAdd = eventsInternal[pvpEventSelection];
                PluginServices.PluginLog.Verbose($"Merging {shoutToAdd}, {eventToAdd}");
                PluginServices.EventShoutcastMapping.MergeShoutcast(eventToAdd, shoutToAdd);
                SaveAndAdd(eventToAdd, shoutToAdd);
            }
            catch (ArgumentOutOfRangeException)
            {
            }

        ImGui.Separator();
        if (ImguiTools.CtrlShiftButton("Reset Custom Mapping"))
        {
            PluginServices.Config.MappingOverride.Clear();
            PluginServices.Config.Save();
            PluginServices.ConfigManager.ReloadConfig();
            PluginServices.ChatGui.Print("Reset Custom Mapping!");
        }
    }

    private void SaveAndAdd(string eventT, string shout)
    {
        var a = PluginServices.JsonLoader.ConvertJsonToMappingDelta(
            PluginServices.Config.MappingOverride.GetValueOrDefault(eventT, "{}"));
        var add = a["add"];
        var remove = a["remove"];
        add.Add(shout);
        remove.Remove(shout);
        var j = PluginServices.JsonLoader.ConvertMappingDeltaToJson(eventT, add, remove);
        PluginServices.Config.MappingOverride[eventT] = j.ToJsonString();
        PluginServices.Config.Save();
        PluginServices.PluginLog.Verbose("Saved: " + j);
    }

    private void SaveAndRemove(string eventT, string shout)
    {
        var a = PluginServices.JsonLoader.ConvertJsonToMappingDelta(
            PluginServices.Config.MappingOverride.GetValueOrDefault(eventT, "{}"));
        var add = a["add"];
        var remove = a["remove"];
        add.Remove(shout);
        remove.Add(shout);
        var j = PluginServices.JsonLoader.ConvertMappingDeltaToJson(eventT, add, remove);
        PluginServices.Config.MappingOverride[eventT] = j.ToJsonString();
        PluginServices.Config.Save();
        PluginServices.PluginLog.Verbose("Saved: " + j);
    }
}