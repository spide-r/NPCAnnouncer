using System;
using System.Runtime.InteropServices;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.Sound;
using InteropGenerator.Runtime;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Impl;

public class PluginSoundManager : ISoundManager
{
    // Attributed to VFXEditor: https://github.com/0ceal0t/Dalamud-VFXEditor/blob/main/VFXEditor/Interop/ResourceLoader.Sound.cs

    private const string PlaySoundSig = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? FE C2";

    private delegate IntPtr PlaySoundDelegate(IntPtr path, byte play);

    private readonly PlaySoundDelegate _playSoundPath;

    private bool _muted;
    //todo - its about time you had another look at this code - different sounds have different volume levels (varies by source, lang, etc)

    private const string InitSoundSig = "E8 ?? ?? ?? ?? 8B 5D 77";

    public unsafe delegate SoundData* InitSoundDelegate(SoundManager* manager, CStringPointer path, float volume,
        uint soundIdx, uint unk1, bool unk2, SoundVolumeCategory category);
    /*
     * [Signature("E8 ?? ?? ?? ?? 48 8B 8D ?? ?? ?? ?? 48 33 CC E8 ?? ?? ?? ?? 48 81 C4 00 05 00 00",
        DetourName = nameof(ProcessPacketActionEffectDetour))]
    private readonly Hook<ProcessPacketActionEffectDelegate> processPacketActionEffectHook = null!;
     */

    public PluginSoundManager()
    {
        PluginServices.GameInteropProvider.InitializeFromAttributes(this);
        _playSoundPath =
            Marshal.GetDelegateForFunctionPointer<PlaySoundDelegate>(PluginServices.SigScanner.ScanText(PlaySoundSig));

        Marshal.GetDelegateForFunctionPointer<InitSoundDelegate>(PluginServices.SigScanner.ScanText(InitSoundSig));
        SetMute(PluginServices.Config.Muted);
        PluginServices.PluginLog.Verbose("Initializing Sound Manager");
    }

    public void PlaySound(string path)
    {
        if (_muted)
        {
            return;
        }

        var bytes = Encoding.ASCII.GetBytes(path);
        var ptr = Marshal.AllocHGlobal(bytes.Length + 1);

        try
        {
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr + bytes.Length, 0);
            _playSoundPath(ptr, 1);
        }
        catch (Exception e)
        {
            PluginServices.PluginLog.Error(e, "Issue In Sound Manager");
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public void ToggleMute()
    {
        _muted = !_muted;
        PluginServices.Config.Muted = _muted;
    }

    public void SetMute(bool mute)
    {
        _muted = mute;
        PluginServices.Config.Muted = _muted;
    }
}