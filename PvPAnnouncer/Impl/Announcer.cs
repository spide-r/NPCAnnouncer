using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI;
using PvPAnnouncer.Data;
using PvPAnnouncer.Interfaces;
using PvPAnnouncer.Interfaces.PvPEvents;
using Enumerable = System.Linq.Enumerable;

namespace PvPAnnouncer.Impl;

public class Announcer : IAnnouncer, IDisposable
{
    /*
     * Objectives:
     * 1. Comment a Reasonable amount
     * 2. Dont repeat voice lines
     * 3. Don't comment on the same thing twice
     * 4. Don't say more than one thing at a time or comment too quickly
     * 5. Use the appropriate gender for the challenger
     */

    //todo rewrite - divide and conquer
    // per announcer %

    private readonly Queue<(string eventStr, DateTime entered)> _lastEvents = new();
    private readonly Queue<(string line, DateTime entered)> _lastVoiceLines = new();
    private readonly Queue<string> _lastTriggers = new();

    private int _lastVoiceLineLength;
    private long _timestamp;
    private readonly IEventShoutcastMapping _eventShoutcastMapping;
    private readonly IShoutcastRepository _shoutcastRepository;
    private readonly Timer _clearTimer;

    public Announcer(IEventShoutcastMapping eventShoutcastMapping, IShoutcastRepository shoutcastRepository)
    {
        _eventShoutcastMapping = eventShoutcastMapping;
        _shoutcastRepository = shoutcastRepository;
        _clearTimer = new Timer(60 * 1000);
        _clearTimer.Elapsed += ClearQueueAfter;
        _clearTimer.Enabled = true;
    }

    public List<string> GetLastTriggers()
    {
        return _lastTriggers.ToList();
    }

    public void ClearQueue()
    {
        _lastVoiceLines.Clear();
        _lastEvents.Clear();
        _timestamp = 0;
    }


    private void ClearQueueAfter(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        while (_lastVoiceLines.Count > 0 && (DateTime.UtcNow - _lastVoiceLines.Peek().entered).TotalSeconds >
               60 * PluginServices.Config.ClearVoicelinesAfter)
        {
            PluginServices.PluginLog.Verbose(
                $"Dequeuing Voice Line {_lastVoiceLines.Peek().line} from history as its older than {PluginServices.Config.ClearVoicelinesAfter} minute(s)");
            _lastVoiceLines.Dequeue();
        }

        while (_lastEvents.Count > 0 && (DateTime.UtcNow - _lastEvents.Peek().entered).TotalSeconds >
               60 * PluginServices.Config.ClearEventsAfter)
        {
            PluginServices.PluginLog.Verbose(
                $"Dequeuing Event {_lastEvents.Peek().eventStr} from history as its older than {PluginServices.Config.ClearEventsAfter} minute(s)");

            _lastEvents.Dequeue();
        }
    }

    private bool ShouldAnnounce() // this function will determine if the settings let us 
    {
        var overworld = PluginServices.Config.Overworld;
        var pvp = PluginServices.Config.PvP;
        var pve = PluginServices.Config.PvE;
        var wd = PluginServices.Config.WolvesDen;
        if (PluginServices.ClientState.IsPvP)
        {
            if (PluginServices.ClientState.IsPvPExcludingDen)
                //in actual pvp zone
                return pvp;

            //in wolves den
            return wd;
        }

        if (PluginServices.DutyState.IsDutyStarted || PluginServices.Condition.Any(ConditionFlag.BoundByDuty,
                ConditionFlag.BoundByDuty56, ConditionFlag.BoundByDuty95))
            //in duty
            return pve;

        //not started and/or in overworld
        return overworld;
    }

    public void PlayAndSendBattleTalkForTesting(Shoutcast shoutcast)
    {
        var p = shoutcast.GetShoutcastSoundPathWithGenderAndLang(PluginServices.Config.Language,
            PluginServices.Config.WantsAttribute("Feminine Pronouns"));

        PlaySound(p);
        SendBattleTalk(shoutcast);
    }

    public void ReceiveEvent(bool bypass, PvPEvent pvpEvent)
    {
        PluginServices.PluginLog.Verbose($"Event {pvpEvent.Id} received");
        long newTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        long diff = newTimestamp - _timestamp;

        if (bypass)
        {
            PluginServices.PluginLog.Verbose("Bypassing randomness & repeat commentary.");
            PlaySoundAndSendBattleTalk(true, pvpEvent);
            return;
        }

        if (!ShouldAnnounce()) return;

        // == Objective 4 ==
        if (diff < (PluginServices.Config.CooldownSeconds + _lastVoiceLineLength))
        {
            PluginServices.PluginLog.Verbose($"Cooldown not finished");
            return;
        }


        if (PluginServices.Condition.Any(ConditionFlag.BoundByDuty,
                ConditionFlag.BoundByDuty56, ConditionFlag.BoundByDuty95) && !PluginServices.DutyState
                .IsDutyStarted) //fixes bug with kardia and other stuff at start of duty but does not allow for weather events
        {
            var id = pvpEvent.Id;
            //duty not started, dont care about wolves den
            if (!(id.Equals("MatchVictoryEvent") || id.Equals("DutyRecommenceEvent") || id.Equals("MatchLossEvent")))
            {
                //not match victory, loss or standard loss - dont want it at the start or at the end of a duty
                PluginServices.PluginLog.Verbose("Duty not started!");
                return;
            }
        }


        int rand = Random.Shared.Next(100);


        // == Objective 1 ==
        if (rand < 100 - PluginServices.Config.Percent)
        {
            PluginServices.PluginLog.Verbose(
                $"Percent not hit. is {rand} when it should be greater than {100 - PluginServices.Config.Percent}");

            return;
        }

        // == Objective 3 == 
        if (FailsRepeatCommentaryCheck(pvpEvent))
        {
            //PluginServices.PluginLog.Verbose($"Repeat commentary check failed");
            return;
        }

        PlaySoundAndSendBattleTalk(pvpEvent);
    }

    public void ReceiveEvent(PvPEvent pvpEvent)
    {
        ReceiveEvent(false, pvpEvent);
    }

    public void PlaySound(string sound)
    {
        if (!PluginServices.DataManager.FileExists(sound))
        {
            PluginServices.PluginLog.Error($"Sound file {sound} not found!");
            return;
        }

        PluginServices.PluginLog.Verbose($"Playing sound: {sound}");
        PluginServices.SoundManager.PlaySound(sound);
    }

    public void SendBattleTalk(Shoutcast shoutcast)
    {
        if (PluginServices.Config.HideBattleText) return;
        var transcription = "";
        PluginServices.PluginLog.Verbose(shoutcast.ToString());
        if (shoutcast.GetTranscriptionWithGender(PluginServices.Config.TextLanguage,
                PluginServices.Config.WantsAttribute("Feminine Pronouns"), PluginServices.SeStringEvaluator).Equals(""))
        {
            if (shoutcast.GetTranscriptionWithGender("en", false, PluginServices.SeStringEvaluator).Equals(""))
            {
                PluginServices.PluginLog.Error($"Text empty for {shoutcast.Shoutcaster}, {shoutcast.SoundPath}");
                return;
            }

            transcription = shoutcast.GetTranscriptionWithGender("en",
                PluginServices.Config.WantsAttribute("Feminine Pronouns"), PluginServices.SeStringEvaluator);
            PluginServices.PluginLog.Warning(
                $"Text empty for {shoutcast.Shoutcaster}, {shoutcast.SoundPath} on lang {PluginServices.Config.TextLanguage} - falling back to EN");
        }
        else
        {
            transcription = shoutcast.GetTranscriptionWithGender(PluginServices.Config.TextLanguage,
                PluginServices.Config.WantsAttribute("Feminine Pronouns"), PluginServices.SeStringEvaluator);
        }

        unsafe
        {
            try
            {
                var name = shoutcast.Shoutcaster;
                var duration = shoutcast.Duration;
                var icon = shoutcast.Icon;
                var style = shoutcast.Style;
                if (icon != 0 && PluginServices.Config.WantsIcon)
                    UIModule.Instance()->ShowBattleTalkImage(name, transcription, duration, icon, style);
                else
                    UIModule.Instance()->ShowBattleTalk(name, transcription, duration, style);
            }
            catch (InvalidOperationException)
            {
                UIModule.Instance()->ShowBattleTalk(InternalConstants.PvPAnnouncerDevName,
                    InternalConstants.ErrorContactDev, 6, 6);
            }
            catch (Exception e)
            {
                PluginServices.PluginLog.Error(e, "Issue sending Battle Talk!");
            }
        }
    }


    private void AddEventToRecentList(PvPEvent e)
    {
        PluginServices.PluginLog.Verbose("Adding Event to history");
        if (_lastEvents.Count > PluginServices.Config.RepeatEventCommentaryQueue - 1)
        {
            PluginServices.PluginLog.Verbose($"Dequeuing Event {_lastEvents.Peek().eventStr} from history");

            _lastEvents.Dequeue();
        }

        _lastEvents.Enqueue((e.Id, DateTime.UtcNow));
    }


    private void AddVoiceLineToRecentList(Shoutcast talk)
    {
        PluginServices.PluginLog.Verbose($"Adding Voice line {talk.SoundPath} to history");

        if (_lastVoiceLines.Count > PluginServices.Config.RepeatVoiceLineQueue - 1)
        {
            PluginServices.PluginLog.Verbose($"Dequeuing Voice Line {_lastVoiceLines.Peek().line} from history");

            _lastVoiceLines.Dequeue();
        }

        _lastVoiceLines.Enqueue((talk.Id, DateTime.UtcNow));
    }

    private bool FailsRepeatCommentaryCheck(PvPEvent pvpEvent)
    {
        var b = _lastEvents.Any(e => e.eventStr.Equals(pvpEvent.Id));
        foreach (var lastEvent in _lastEvents)
        {
            PluginServices.PluginLog.Verbose($"Last Event: {lastEvent.eventStr}");
        }

        PluginServices.PluginLog.Verbose($"Repeat commentary check triggered - value is {b}");
        return b;
    }


    private void PlaySoundAndSendBattleTalk(bool bypass, PvPEvent pvpEvent)
    {
        List<Shoutcast> sounds = [];

        foreach (var shoutcastId in _eventShoutcastMapping.GetShoutcastList(pvpEvent.Id))
        {
            var sound = _shoutcastRepository.GetShoutcast(shoutcastId);
            if (sound == null)
            {
                PluginServices.PluginLog.Warning($"{shoutcastId} not found.");
                continue;
            }

            if (!PluginServices.DataManager.FileExists(
                    sound.GetShoutcastSoundPathWithLang(PluginServices.Config.Language)))
            {
                PluginServices.PluginLog.Error($"Sound file {sound} not found!");
                continue;
            }

            if (PluginServices.Config.MutedShouts.Contains(shoutcastId)) continue;

            // == Objective 5 == 
            if (PluginServices.Config.WantsAllAttributes(sound.Attributes) &&
                PluginServices.Config.WantsAttribute(sound.Shoutcaster))
            {
                if (sound.IsGendered)
                {
                    var masc = PluginServices.Config.WantsAttribute("Masculine Pronouns");
                    var fem = PluginServices.Config.WantsAttribute("Feminine Pronouns");
                    if (masc || fem) // voice is gendered and user wants at least 1
                    {
                        sounds.Add(sound);
                    }
                }
                else
                {
                    sounds.Add(sound);
                }
            }
        }

        // == Objective 2 == 
        if (!bypass)
        {
            foreach (var sound in Enumerable.Where(Enumerable.ToList(sounds),
                         sound => _lastVoiceLines.Any(s => s.line.Equals(sound.Id))))
            {
                sounds.Remove(sound);
            }
        }

        if (sounds.Count < 1)
        {
            PluginServices.PluginLog.Verbose(
                $"Sound list after customization removal is less than 1 for {pvpEvent.Id}");
            return;
        }

        int rand = Random.Shared.Next(sounds.Count);
        var s = sounds[rand];
        WrapUp(pvpEvent, s);
        PluginServices.Framework.RunOnTick(async () =>
        {
            PluginServices.PluginLog.Verbose(
                $"Playing announcement (Delaying): {s.SoundPath} by {PluginServices.Config.AnimationDelayFactor}");
            await Task.Delay(PluginServices.Config
                .AnimationDelayFactor); //delay to prevent shenanigans w/ attacks being announced before their animations finish
            var p = s.GetShoutcastSoundPathWithGenderAndLang(PluginServices.Config.Language,
                PluginServices.Config.WantsAttribute("Feminine Pronouns"));
            PlaySound(p);
            SendBattleTalk(s);
            PluginServices.PluginLog.Verbose($"Finished Playing announcement after delay: {s}");
        });
    }

    public void PlaySoundAndSendBattleTalk(PvPEvent pvpEvent)
    {
        PlaySoundAndSendBattleTalk(false, pvpEvent);
    }

    private void WrapUp(PvPEvent pvpEvent, Shoutcast? chosenLine)
    {
        AddEventToRecentList(pvpEvent);
        if (chosenLine != null)
        {
            _lastTriggers.Enqueue($"{pvpEvent.Id} -> {chosenLine.Id}");
            if (_lastTriggers.Count > 10) _lastTriggers.Dequeue();

            AddVoiceLineToRecentList(chosenLine);
            _lastVoiceLineLength = chosenLine.Duration;
        }

        _timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
    }

    public void Dispose()
    {
        _clearTimer.Enabled = false;
        _clearTimer.Dispose();
    }
}