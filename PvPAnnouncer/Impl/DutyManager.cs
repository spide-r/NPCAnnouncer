using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.DutyState;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PvPAnnouncer.Impl.Messages;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Impl;

public class DutyManager : IDutyManager, IEventPublisher
{
    private int _leftPoints = 0;
    private int _rightPoints = 0;
    private int _ourPoints = 0;
    private double _ourProgress = 0.0;
    private double _enemyProgress = 0.0;
    private readonly IPlayerStateTracker _playerState;

    public DutyManager(IPlayerStateTracker playerState)
    {
        _playerState = playerState;
        PluginServices.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "PvPFrontlineHeader", HandleHeaderPreDraw);
        PluginServices.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "PvPMKSHeader", HandleCCHeaderPreDraw);
        PluginServices.ClientState.TerritoryChanged += ClientStateOnTerritoryChanged;
        PluginServices.ClientState.EnterPvP += EnterPvP;
        PluginServices.ClientState.CfPop += ClientStateOnCfPop;
        PluginServices.DutyState.DutyStarted += DutyStarted;
        PluginServices.DutyState.DutyRecommenced += DutyStateOnDutyRecommenced;

        PluginServices.DutyState.DutyCompleted += DutyCompleted;
    }

    private void DutyStateOnDutyRecommenced(IDutyStateEventArgs args)
    {
        EmitToBroker(new DutyRecommenceMessage());
    }

    private void HandleCCHeaderPreDraw(AddonEvent type, AddonArgs args)
    {
        unsafe
        {
            try
            {
                //7.5 - nodes were offset by 2
                var addon = (AtkUnitBase*) args.Addon.Address;
                var astraPercent = addon->GetTextNodeById(53)->NodeText.ToString();
                astraPercent = astraPercent.Substring(0, astraPercent.Length - 1);
                var umbraPercent = addon->GetTextNodeById(54)->NodeText.ToString();
                umbraPercent = umbraPercent.Substring(0, umbraPercent.Length - 1);

                //todo explain what im doing here cause this is unacceptable 
                var astraColor =
                    addon->GetTextNodeById(51)->EdgeColor
                        .RGBA;
                var umbraColor = addon->GetTextNodeById(52)->EdgeColor.RGBA;
                //4286996785
                // astra was blue, umbra was red
                //08:20:48.837 | VRB | [PvPAnnouncer] 0.0 (4286996785) - 0.0 (4281348230)
                if (astraColor == 4286996785)
                {
                    _ourProgress = double.Parse(astraPercent);
                    _enemyProgress = double.Parse(umbraPercent);
                }
                else
                {
                    _ourProgress = double.Parse(umbraPercent);
                    _enemyProgress = double.Parse(astraPercent);
                }
            }
            catch (Exception e)
            {
                PluginServices.PluginLog.Error(e, "Issue Reading CC Header!");
            }
        }
    }

    private void HandleHeaderPreDraw(AddonEvent type, AddonArgs args)
    {
        unsafe
        {
            try
            {
                var addon = (AtkUnitBase*) args.Addon.Address;

                _ourPoints = GetScore(9, addon);
                _rightPoints = GetScore(13, addon);
                _leftPoints = GetScore(12, addon);
            }
            catch (Exception e)
            {
                PluginServices.PluginLog.Error(e, "Issue Reading FL Header!");
            }
        }
    }

    private unsafe int GetScore(uint id, AtkUnitBase* addon)
    {
        var alliance = addon->GetComponentNodeById(id)->GetComponent();
        var scoreNode = alliance->GetComponentById(8);
        var currentScoreNode = scoreNode->GetTextNodeById(2)->GetText().ToString();
        return Convert.ToInt32(currentScoreNode);
    }

    private void EnterPvP()
    {
        PluginServices.PluginLog.Verbose("EnterPvP");
        _playerState.CheckSoundState();
    }

    private void ClientStateOnCfPop(ContentFinderCondition obj)
    {
        PluginServices.PluginLog.Verbose("OnCfPop");
    }

    private void ClientStateOnTerritoryChanged(uint territory)
    {
        var t = PluginServices.DataManager.GetExcelSheet<TerritoryType>().GetRow(territory);
        PluginServices.PluginLog.Verbose("OnTerritoryChanged " + territory);
        if (t.IsPvpZone)
        {
            if (territory == 250) // entering the wolves den
            {
                if (PluginServices.PlayerStateTracker.IsPvP()) // Went from pvp area to wolves den 
                {
                    PluginServices.PluginLog.Verbose("Territory Change: PvP -> WD");
                    PvPMatchLeft();
                }
            }
            else
            {
                PluginServices.PluginLog.Verbose("Territory Change: NoPvP->PvP");
                PvPMatchEntered(territory); // entered a pvp zone that isnt the wolves den
            }
        }
        else
        {
            if (PluginServices.PlayerStateTracker
                .IsPvP()) // was in pvp zone before warp and then warped to a non-pvp zone
            {
                PluginServices.PluginLog.Verbose("Territory Change: PvP -> NoPvP");
                PvPMatchLeft();
            }
        }
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

    private void CheckWin(bool w)
    {
        if (w)
        {
            EmitToBroker(new MatchVictoryMessage());
        }
        else
        {
            EmitToBroker(new MatchLossMessage());
        }
    }


    public void PvPMatchEntered(uint territory)
    {
        _ourPoints = 0;
        _rightPoints = 0;
        _leftPoints = 0;
        _ourProgress = 0.0;
        _enemyProgress = 0.0;
        PluginServices.PluginLog.Verbose($"OnMatchEntered {territory}");
        EmitToBroker(new MatchEnteredMessage((ushort) territory));
        unsafe
        {
            var weatherId = WeatherManager.Instance()->GetWeatherForDaytime((ushort) territory, 0);
            PluginServices.PluginLog.Verbose($"Match Weather: {weatherId}");
            EmitToBroker(new MatchWeatherMessage(weatherId));
        }
    }

    public void DutyStarted(IDutyStateEventArgs args)
    {
        PluginServices.Announcer.ClearQueue(); // new duty - fully reset everything
        EmitToBroker(new MatchStartedMessage());
    }

    public void DutyCompleted(IDutyStateEventArgs args)
    {
        if (_ourPoints > 0 || _rightPoints > 0 || _leftPoints > 0)
        {
            CheckWin(_ourPoints >= _leftPoints && _ourPoints >= _rightPoints);
        }
        else if (_enemyProgress > 0.0 || _ourProgress > 0.0)
        {
            CheckWin(_ourProgress >= _enemyProgress);
        }
        else
        {
            EmitToBroker(new MatchVictoryMessage()); // pve - default to victory
        }
    }

    public void PvPMatchLeft()
    {
        EmitToBroker(new MatchLeftMessage());
    }

    public void EmitToBroker(IMessage pvpEvent)
    {
        PluginServices.PvPEventBroker.IngestMessage(pvpEvent);
    }

    public void Dispose()
    {
        PluginServices.ClientState.TerritoryChanged -= ClientStateOnTerritoryChanged;
        PluginServices.ClientState.EnterPvP -= EnterPvP;
        PluginServices.ClientState.CfPop -= ClientStateOnCfPop;
        PluginServices.DutyState.DutyStarted -= DutyStarted;
        PluginServices.DutyState.DutyCompleted -= DutyCompleted;
        PluginServices.DutyState.DutyRecommenced -= DutyStateOnDutyRecommenced;
        PluginServices.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, "PvPFrontlineHeader", HandleHeaderPreDraw);
        PluginServices.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, "PvPMKSHeader", HandleCCHeaderPreDraw);
    }
}