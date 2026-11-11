using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using PvPAnnouncer.Data;

namespace PvPAnnouncer
{
    //there is so much differing naming for everything, id/internalname/actionid shout/annnounce fix it!!!


    public sealed class PvPAnnouncerPlugin : IDalamudPlugin
    {
        private readonly WindowSystem WindowSystem = new("NPCAnnouncer");

        public PvPAnnouncerPlugin(IDalamudPluginInterface pluginInterface)
        {
            PluginServices.Initialize(pluginInterface, WindowSystem);
            LoadCommands();

            pluginInterface.UiBuilder.Draw += DrawUi;
            pluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
            pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigWindow;
            PluginServices.ClientState.Login += PluginUpdateMessage;
            PluginUpdateMessage();
        }

        public static void PluginUpdateMessage()
        {
            if (!PluginServices.Config.SeenUpdateMessages.Contains(InternalConstants.CurrentUpdateMessage))
            {
                if (!PluginServices.ClientState.IsLoggedIn)
                {
                    return;
                }

                PluginServices.ChatGui.Print(InternalConstants.CurrentUpdateMessage,
                    InternalConstants.MessageTag, 15);
                PluginServices.Config.SeenUpdateMessages.Add(InternalConstants.CurrentUpdateMessage);
                PluginServices.Config.Save();
            }
        }

        private void DrawUi()
        {
            WindowSystem.Draw();
        }

        private void OnCommand(string command, string args)
        {
            ToggleConfigWindow();
        }

        private void OnCommandPvP(string command, string args)
        {
            PluginServices.ChatGui.Print(
                "Please use /npcannouncer, the old command /pvpannouncer will be removed soon.",
                InternalConstants.MessageTag, 15);
            ToggleConfigWindow();
        }

//
        private void OnToggleMuteCommand(string command, string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                PluginServices.SoundManager.ToggleMute();
                var un = "";
                if (!PluginServices.Config.Muted) un = "un-";

                PluginServices.ChatGui.Print("All NPC Announcers have been " + un + "muted!",
                    InternalConstants.MessageTag);
            }
            else
            {
                args = args.Trim();
                if (PluginServices.ShoutcastRepository.GetShoutcasters()
                    .Contains(args))
                {
                    // valid shoutcaster
                    if (PluginServices.Config.DesiredAttributes.Contains(args))
                    {
                        PluginServices.ChatGui.Print($"{args} has been disabled.", InternalConstants.MessageTag);
                        PluginServices.Config.DesiredAttributes.Remove(args);
                    }
                    else
                    {
                        PluginServices.ChatGui.Print($"{args} has been enabled.", InternalConstants.MessageTag);
                        PluginServices.Config.DesiredAttributes.Add(args);
                    }
                }
                else
                {
                    PluginServices.ChatGui.Print(
                        $"{args} does not seem to be a valid announcer. Please double check you have proper spelling and casing.");
                }
            }

            PluginServices.Config.Save();
        }

        private void ToggleConfigWindow()
        {
            PluginServices.ConfigWindow.Toggle();
        }

        private void ToggleMainUI()
        {
            PluginUpdateMessage();
            PluginServices.ConfigWindow.Toggle();
        }

        private void LoadCommands()
        {
            PluginServices.CommandManager.AddHandler("/pvpannouncer", new CommandInfo(OnCommandPvP)
            {
                HelpMessage = "Open the Config Window - Old Command! Use /npcannouncer"
            });

            PluginServices.CommandManager.AddHandler("/npcannouncer", new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the Config Window"
            });
            PluginServices.CommandManager.AddHandler("/muteannouncer", new CommandInfo(OnToggleMuteCommand)
            {
                HelpMessage = "Toggle Mute Announcer - Supply a name to this command to toggle a specified announcer!"
            });
        }

        private void UnloadCommands()
        {
            PluginServices.CommandManager.RemoveHandler("/pvpannouncer");
            PluginServices.CommandManager.RemoveHandler("/npcannouncer");
            PluginServices.CommandManager.RemoveHandler("/muteannouncer");
        }

        public void Dispose()
        {
            PluginServices.ClientState.Login -= PluginUpdateMessage;
            PluginServices.DalamudPluginInterface.UiBuilder.Draw -= DrawUi;
            PluginServices.DalamudPluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
            PluginServices.DalamudPluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigWindow;
            PluginServices.EventHooksPublisher.Dispose();
            PluginServices.PlayerStateTracker.Dispose();
            PluginServices.DutyManager.Dispose();
            PluginServices.Announcer.Dispose();
            UnloadCommands();
        }
    }
}