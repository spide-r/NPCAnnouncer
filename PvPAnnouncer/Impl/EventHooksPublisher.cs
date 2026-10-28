using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using PvPAnnouncer.Data;
using PvPAnnouncer.Impl.Messages;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Impl;

public class EventHooksPublisher : IEventPublisher, IDisposable
{
    private unsafe delegate void ProcessPacketActionEffectDelegate(
        int sourceId, IntPtr sourceCharacter, IntPtr pos, ActionEffectHeader* effectHeader, ActionEffect* effectArray,
        ulong* effectTrail);

    private delegate void ProcessPacketActorControlDelegate(uint entityId, uint type, uint statusId, uint amount,
        uint a5, uint source, uint a7, uint a8, uint param7New, uint param8New, ulong targetId, byte flag);
    //uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, uint param7, uint param8, ulong targetId, byte param9

    [Signature("E8 ?? ?? ?? ?? 48 8B 8D ?? ?? ?? ?? 48 33 CC E8 ?? ?? ?? ?? 48 81 C4 00 05 00 00",
        DetourName = nameof(ProcessPacketActionEffectDetour))]
    private readonly Hook<ProcessPacketActionEffectDelegate> processPacketActionEffectHook = null!;

    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ProcessPacketActorControlDetour))]
    private readonly Hook<ProcessPacketActorControlDelegate> processPacketActorControlHook = null!;

    private unsafe void ProcessPacketActionEffectDetour(int sourceId, IntPtr sourceCharacter, IntPtr pos,
        ActionEffectHeader* effectHeader,
        ActionEffect* effectArray, ulong* effectTrail)
    {
        processPacketActionEffectHook.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
        try
        {
            ActionEffectMessage actionEffect = new ActionEffectMessage(sourceId, sourceCharacter, pos, effectHeader,
                effectArray, effectTrail);
            foreach (var targetId in actionEffect.GetTargetIds())
                //value 1789 for vuln up
                if (PluginServices.DutyManager.IsMonitoredUser(targetId))
                    foreach (var actionEffectVar in actionEffect.GetEffects(targetId))
                    {
                        PluginServices.PluginLog.Verbose(
                            $"Applied Status on {targetId}: {actionEffectVar.EffectType}, {actionEffectVar.Flags1}, {actionEffectVar.Flags2} {actionEffectVar.Param0} {actionEffectVar.Param1} {actionEffectVar.Param2} {actionEffectVar.Value}");
                        //w/ effecttype damage status - value is the amount of damage
                        // w/ effecttype applystatuseffecttarget - value is status
                        //applystatuseffectsource - self buff, but only sometimes
                        //w/ effecttype kbanddrawin - could be sucked in  

                        //todo could do a blocked damage event 
                        if (actionEffectVar.EffectType ==
                            ActionEffectType
                                .ApplyStatusEffectTarget) //someone applied a status effect to our monitored user
                        {
                            var effectValue = actionEffectVar.Value;
                            EmitToBroker(new AppliedStatusMessage(effectValue));
                        }
                    }


            var s = "S:" + actionEffect.SourceId + " SN: " + actionEffect.GetSource() + "|A: " +
                    actionEffect.ActionId + "|AN: " +
                    actionEffect.GetAction()?.Name.ToString() + " |ET: " + "EA: ";
            PluginServices.PluginLog.Verbose(s);
            EmitToBroker(actionEffect);
        }
        catch (Exception e)
        {
            PluginServices.PluginLog.Error(e, "Problem when Processing Action Effect");
        }
    }

    //GOTCHA: some of these are labeled incorrectly but we only care about entityid and type 
    //Actor Control is much much more complicated than previously thought, apparently theres 3 different ways each of these args can be used - yikes!
    //https://github.com/awgil/ffxiv_bossmod/blob/ccd339625b6d5f561cfccde1f82aeff3693c67ea/BossMod/Network/ServerIPC.cs#L503
    private void ProcessPacketActorControlDetour(uint entityId, uint category, uint arg1, uint arg2, uint arg3,
        uint arg4, uint arg5, uint arg6, uint arg7, uint arg8, ulong targetId, byte isRecorded)
    {
        processPacketActorControlHook.Original(entityId, category, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8,
            targetId, isRecorded);
        try
        {
            var actorControlMessage =
                new ActorControlMessage(entityId, category);


            if (!PluginServices.DutyManager.IsMonitoredUser(entityId))
            {
                return;
            }

            PluginServices.PluginLog.Verbose(
                $"Processing Actor Control entityId {entityId},  category {(ActorControlCategory) category}, arg1 {arg1}, " +
                $"a2 {arg2}, a3 {arg3}, a4 {arg4}, a5 {arg5}, a6 {arg6}, a7 {arg7}, a8 {arg8}, targetId {targetId}, flag {isRecorded}");

            if (actorControlMessage.GetCategory() == ActorControlCategory.GainEffect)
            {
                if (arg1 == StatusIds.BH5)
                {
                    EmitToBroker(new BattleHighMessage(5));
                }
                else if (arg1 == StatusIds.BH4)
                {
                    EmitToBroker(new BattleHighMessage(4));
                }
                else if (arg1 == StatusIds.BH3)
                {
                    EmitToBroker(new BattleHighMessage(3));
                }
                else if (arg1 == StatusIds.BH2)
                {
                    EmitToBroker(new BattleHighMessage(2));
                }
                else if (arg1 == StatusIds.BH1)
                {
                    EmitToBroker(new BattleHighMessage(1));
                }
                else if (arg1 == StatusIds.FlyingHigh)
                {
                    EmitToBroker(new FlyingHighMessage());
                }
                else if (arg1 == StatusIds.Soaring)
                {
                    EmitToBroker(new SoaringMessage((int) arg2));
                }
            }
            else if (category == 80) //this might be a gotcha - is effect 80 guaranteed to be a zone out?
            {
                PluginServices.PluginLog.Debug("Category 80!!!! Zone out!");
                EmitToBroker(new UserZoneOutMessage());
            }
            else
            {
                EmitToBroker(actorControlMessage);
            }
        }
        catch (Exception e)
        {
            PluginServices.PluginLog.Error(e, "Problem when Processing Actor Control");
        }
    }

    public EventHooksPublisher()
    {
        PluginServices.GameInteropProvider.InitializeFromAttributes(this);
        processPacketActionEffectHook.Enable();
        processPacketActorControlHook.Enable();
    }

    public void EmitToBroker(IMessage pvpEvent)
    {
        PluginServices.PvPEventBroker.IngestMessage(pvpEvent);
    }

    public void Dispose()
    {
        processPacketActionEffectHook.Disable();
        processPacketActorControlHook.Disable();

        processPacketActionEffectHook.Dispose();
        processPacketActorControlHook.Dispose();
    }
}