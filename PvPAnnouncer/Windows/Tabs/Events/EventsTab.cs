using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using OtterGui.Widgets;

namespace PvPAnnouncer.Windows.Tabs.Events;

//enable, disable, test, etc
public class EventsTab : ITab
{
    private readonly Configuration _configuration = PluginServices.Config;
    private int _activeEventsSelectedItem;
    private int _disabledEventsSelectedItem;
    private string[] _activeEventsArr = [];
    private string[] _activeEventsArrInternal = [];
    private string[] _disabledEventsArr = [];
    private string[] _disabledEventsArrInternal = [];
    private int _selection;
    private string _shoutcaster = "";
    public ReadOnlySpan<byte> Label => "Events"u8;

    public void DrawContent()
    {
        var blEvents = _configuration.BlacklistedEvents;

        if (ImGui.CollapsingHeader("Enable & Disable Events"))
        {
            var activeEvents = new List<string>();
            var activeEventsInternal = new List<string>();
            foreach (var e in PluginServices.PvPEventBroker.GetPvPEvents())
            {
                var eventId = e.Id;
                if (!blEvents.Contains(eventId))
                {
                    activeEvents.Add(e.Name);
                    activeEventsInternal.Add(eventId);
                }
            }


            List<string> listDisabledInternal = [];
            List<string> listDisabledPublic = [];
            foreach (var internalName in blEvents)
            {
                var e = PluginServices.PvPEventBroker.GetEvent(internalName);
                if (e == null) continue;

                listDisabledInternal.Add(internalName);
                listDisabledPublic.Add(e.Name);
            }

            _activeEventsArr = activeEvents.ToArray();
            _activeEventsArrInternal = activeEventsInternal.ToArray();
            ImGui.Text("Enabled Events:");
            ImGui.ListBox("###EnabledEvents", ref _activeEventsSelectedItem, _activeEventsArr);
            if (ImGui.Button("Disable"))
                if (_activeEventsSelectedItem < _activeEventsArrInternal.Length)
                {
                    _configuration.BlacklistedEvents.Add(_activeEventsArrInternal[_activeEventsSelectedItem]);
                    _configuration.Save();
                }

            _disabledEventsArrInternal = listDisabledInternal.ToArray();
            _disabledEventsArr = listDisabledPublic.ToArray();
            ImGui.Text("Disabled Events:");
            ImGui.ListBox("###DisabledEvents", ref _disabledEventsSelectedItem, _disabledEventsArr);
            if (ImGui.Button("Enable"))
                if (_disabledEventsSelectedItem < _disabledEventsArrInternal.Length)
                {
                    _configuration.BlacklistedEvents.Remove(_disabledEventsArrInternal[_disabledEventsSelectedItem]);
                    _configuration.Save();
                }
        }

        if (ImGui.CollapsingHeader("Event Tester###Testerheader")) EventTester();


        if (ImGui.CollapsingHeader("Event Queue###QueueHeader")) EventQueue();
    }

    private void EventTester()
    {
        ImGui.Text("Event Tester");
        ImGui.TextWrapped(
            "This Simulates these events happening in real pvp, using your configuration settings (except for cooldown between announcements.");
        var i = 1;
        foreach (var ev in PluginServices.PvPEventBroker.GetPvPEvents())
        {
            if (ImGui.Button(ev.Name))
                try
                {
                    PluginServices.Announcer.ReceiveEvent(true, ev);
                    PluginServices.Announcer.ClearQueue();
                }
                catch (Exception e)
                {
                    PluginServices.PluginLog.Error(e, "Issue sending custom event!!!");
                }

            if (i % 4 != 0) ImGui.SameLine();

            i++;
        }
    }

    private void EventQueue()
    {
        ImGui.TextWrapped("Here are the last 10 triggered events and their voicelines:");
        foreach (var lastTrigger in PluginServices.Announcer.GetLastTriggers()) ImGui.TextWrapped(lastTrigger);
    }
}