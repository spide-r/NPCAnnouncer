using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.DutyState;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PvPAnnouncer.Impl.Messages;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Impl;

public class DutyManager : IDutyManager, IEventPublisher
{
    private readonly IPlayerStateTracker _playerState;

    public DutyManager(IPlayerStateTracker playerState)
    {
        _playerState = playerState;
        PluginServices.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "FrontlineRecord",
            HandleFrontlineRecordPostSetup);
        PluginServices.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MKSRecord", HandleCCRecordPostSetup);
        PluginServices.ClientState.EnterPvP += EnterPvP;
        PluginServices.ClientState.CfPop += ClientStateOnCfPop;
        PluginServices.DutyState.DutyStarted += DutyStarted;
        PluginServices.DutyState.DutyRecommenced += DutyStateOnDutyRecommenced;
        PluginServices.DutyState.DutyWiped += DutyStateOnDutyWiped;
        PluginServices.DutyState.DutyCompleted += DutyCompleted;
    }

    private void HandleCCRecordPostSetup(AddonEvent type, AddonArgs args)
    {
        unsafe
        {
            try
            {
                var addon = (AtkUnitBase*) args.Addon.Address;
                for (uint i = 7; i <= 8; i++) // astra, umbra
                {
                    var panel = addon->GetComponentNodeById(i)->GetComponent();
                    var ourTeam = panel->GetNodeById(3)->IsVisible();

                    if (ourTeam)
                    {
                        var group1 = panel->GetNodeById(4)->IsVisible(); //group 1 is node id 4, group 2 is node id 8
                        //groups are visible depending on if your team is blue or red 
                        var victory = false;
                        if (group1)
                            victory = panel->GetNodeById(5)->IsVisible(); //victory text for group 1 is node 5
                        else
                            victory = panel->GetNodeById(9)->IsVisible(); //victory text for group 2 is node 9

                        if (victory)
                            EmitToBroker(new MatchVictoryMessage());
                        else
                            EmitToBroker(new MatchLossMessage());
                    }
                }
            }
            catch (Exception e)
            {
                PluginServices.PluginLog.Error(e, "Issue Reading CC Record!");
            }
        }
    }

    private void HandleFrontlineRecordPostSetup(AddonEvent type, AddonArgs args)
    {
        unsafe
        {
            try
            {
                var addon = (AtkUnitBase*) args.Addon.Address;
                for (uint i = 7; i <= 9; i++) //mael, adders, flames
                {
                    var panel = addon->GetComponentNodeById(i)->GetComponent();
                    var ourTeam = panel->GetNodeById(3)->IsVisible();
                    if (ourTeam)
                    {
                        var placement = panel->GetImageNodeById(5)->PartId;
                        var score = panel->GetTextNodeById(10)->NodeText.ToString();
                        PluginServices.PluginLog.Verbose($"Our place is {placement} - Score is {score}");
                        if (placement == 0) // 0 = 1st, 1 = 2nd, 2 = 3rd 
                            EmitToBroker(new MatchVictoryMessage());
                        else
                            EmitToBroker(new MatchLossMessage());
                    }
                }
            }
            catch (Exception e)
            {
                PluginServices.PluginLog.Error(e, "Issue Reading FL Record!");
            }
        }
    }

    private void DutyStateOnDutyWiped(IDutyStateEventArgs args)
    {
        EmitToBroker(new MatchLossMessage());
    }

    private void DutyStateOnDutyRecommenced(IDutyStateEventArgs args)
    {
        EmitToBroker(new DutyRecommenceMessage());
    }

    private void EnterPvP()
    {
        PluginServices.PluginLog.Verbose("EnterPvP");
        _playerState.CheckSoundState();
    }

    private void ClientStateOnCfPop(ContentFinderCondition obj)
    {
        PluginServices.PluginLog.Verbose("OnCfPop");
        EmitToBroker(new DutyPopMessage());
    }

    public bool IsMonitoredUser(int userId)
    {
        return IsMonitoredUser((uint) userId);
    }

    public bool IsMonitoredUser(uint entityId)
    {
        if (PluginServices.ObjectTable.LocalPlayer != null &&
            entityId == PluginServices.PlayerState.EntityId) return true;

        if (PluginServices.PlayerStateTracker.IsPvP() && PluginServices.Config.PartyMembersPvP)
            return PlayerInParty(entityId);

        if (!PluginServices.PlayerStateTracker.IsPvP() && PluginServices.Config.PartyMembersPvE)
            return PlayerInParty(entityId);

        return false;
    }

    private bool PlayerInParty(uint entityId)
    {
        return PluginServices.PartyList.Any(partyMember => partyMember.EntityId == entityId);
    }


    public void DutyStarted(IDutyStateEventArgs args)
    {
        PluginServices.Announcer.ClearQueue(); // new duty - fully reset everything
        EmitToBroker(new MatchStartedMessage());
    }

    public void DutyCompleted(IDutyStateEventArgs args)
    {
        if (!PluginServices.ClientState.IsPvP)
        {
            EmitToBroker(new MatchVictoryMessage()); // pve - default to victory
        }
    }

    public void EmitToBroker(IMessage pvpEvent)
    {
        PluginServices.PvPEventBroker.IngestMessage(pvpEvent);
    }

    public void Dispose()
    {
        PluginServices.ClientState.EnterPvP -= EnterPvP;
        PluginServices.ClientState.CfPop -= ClientStateOnCfPop;
        PluginServices.DutyState.DutyStarted -= DutyStarted;
        PluginServices.DutyState.DutyCompleted -= DutyCompleted;
        PluginServices.DutyState.DutyRecommenced -= DutyStateOnDutyRecommenced;
        PluginServices.DutyState.DutyWiped -= DutyStateOnDutyWiped;

        PluginServices.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "FrontlineRecord",
            HandleFrontlineRecordPostSetup);
        PluginServices.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "MKSRecord", HandleCCRecordPostSetup);
    }
}