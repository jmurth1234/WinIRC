using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using WinIRC.Net;

namespace WinIRC.Commands
{
    public class CommandHandler
    {
        public delegate void Command(Irc irc, string[] args);
        private Dictionary<String, Command> CommandTable = new Dictionary<String, Command>();
        private List<string> CommandList = new List<string>();

        public CommandHandler()
        {
            RegisterCommand("/help", HelpCommandHandler);

            RegisterCommand("/me", MeCommandHandler);
            RegisterCommand("/join", JoinCommandHandler);
            RegisterCommand("/part", PartCommandHandler);
            RegisterCommand("/quit", QuitCommandHandler);
            RegisterCommand("/query", QueryCommandHandler);
            RegisterCommand("/msg", MsgCommandHandler);
            RegisterCommand("/whois", WhoisCommandHandler);

            RegisterCommand("/mode", ModeCommandHandler);
            RegisterCommand("/op", OpCommandHandler);
            RegisterCommand("/deop", OpCommandHandler);
            RegisterCommand("/voice", VoiceCommandHandler);
            RegisterCommand("/devoice", VoiceCommandHandler);

            RegisterCommand("/mute", QuietCommandHandler);
            RegisterCommand("/unmute", QuietCommandHandler);

            RegisterCommand("/kick", KickCommandHandler);
            RegisterCommand("/ban", BanCommandHandler);

            RegisterCommand("/raw", RawCommandHandler);
        }

        internal void RegisterCommand(string cmd, Command handler)
        {
            CommandTable.Add(cmd, handler);
            CommandList.Add(cmd);
        }

        private void HelpCommandHandler(Irc irc, string[] args)
        {
            irc.ClientMessage("The following commands are available: ");
            irc.ClientMessage(String.Join(", ", CommandList));
        }


        internal void QuietCommandHandler(Irc irc, string[] args)
        {
            if (args.Length != 2)
            {
                irc.ClientMessage("Wrong params: " + String.Join(" ", args));
                return;
            }

            string[] modeArgs;
            if (args[0].ToLower().Contains("unmute"))
            {
                modeArgs = new string[] { "MODE", irc.currentChannel, "-q", args[1] + "!*@*" };
            }
            else
            {
                modeArgs = new string[] { "MODE", irc.currentChannel, "+q", args[1] + "!*@*" };
            }

            ModeCommandHandler(irc, modeArgs);
        }


        internal void BanCommandHandler(Irc irc, string[] args)
        {
            if (args.Length != 2)
            {
                irc.ClientMessage("Wrong params: " + String.Join(" ", args));
                return;
            }

            string[] modeArgs;

            modeArgs = new string[] { "MODE", irc.currentChannel, "+b", args[1] + "!*@*" };

            ModeCommandHandler(irc, modeArgs);
        }


        internal void WhoisCommandHandler(Irc irc, string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            irc.WriteLine("WHOIS " + args[1]);
        }

        internal void QueryCommandHandler(Irc irc, string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            irc.AddChannel(args[1]);
        }

        internal void KickCommandHandler(Irc irc, string[] args)
        {
            if (args.Length < 2)
            {
                return;
            }

            var nick = args[1];
            var kick = "KICK " + irc.currentChannel + " " + nick;

            if (args.Length > 3)
            {
                kick += " :" + String.Join(" ", args, 2, args.Length - 2);
            }

            irc.WriteLine(kick);
        }

        internal void MsgCommandHandler(Irc irc, string[] args)
        {
            if (args.Length < 3)
            {
                return;
            }

            var nick = args[1];
            var msg = "PRIVMSG " + nick;

            msg += " :" + String.Join(" ", args, 2, args.Length - 2);

            irc.WriteLine(msg);
        }


        internal void RawCommandHandler(Irc irc, string[] args)
        {
            if (args.Length == 1)
            {
                return;
            }
            var message = String.Join(" ", args, 1, args.Length - 1);
            irc.WriteLine(message);
        }

        internal void OpCommandHandler(Irc irc, string[] args)
        {
            if (args.Length != 2)
            {
                irc.ClientMessage("Wrong params: " + String.Join(" ", args));
                return;
            }

            string[] modeArgs;
            if (args[0].ToLower().Contains("deop"))
            {
                modeArgs = new string[] { "MODE", irc.currentChannel, "-o", args[1] };
            }
            else
            {
                modeArgs = new string[] { "MODE", irc.currentChannel, "+o", args[1] };
            }

            ModeCommandHandler(irc, modeArgs);
        }

        internal void VoiceCommandHandler(Irc irc, string[] args)
        {
            if (args.Length != 2)
            {
                irc.ClientMessage("Wrong params: " + String.Join(" ", args));
                return;
            }

            string[] modeArgs;
            if (args[0].ToLower().Contains("devoice"))
            {
                modeArgs = new string[] { "MODE", irc.currentChannel, "-v", args[1] };
            }
            else
            {
                modeArgs = new string[] { "MODE", irc.currentChannel, "+v", args[1] };
            }

            ModeCommandHandler(irc, modeArgs);
        }

        internal void ModeCommandHandler(Irc irc, string[] args)
        {
            if ((args.Length < 3) || (args.Length > 2 && !args[1].StartsWith("#")))
            {
                irc.ClientMessage("Command too short!");
                return;
            }

            var modeLine = "MODE ";

            if (args[1].StartsWith("#"))
            {
                modeLine += args[1] + " " + args[2];

                if (args.Length == 4)
                {
                    modeLine += " " + args[3];
                }
            }    
            else
            {
                modeLine += irc.currentChannel + " " + args[1];

                if (args.Length == 3)
                {
                    modeLine += " " + args[2];
                }

            }

            Debug.WriteLine(modeLine);

            irc.WriteLine(modeLine);
        }

        internal void MeCommandHandler(Irc irc, string[] args)
        {
            if (args.Length == 1)
            {
                return;
            }
            var message = String.Join(" ", args, 1, args.Length - 1);
            irc.SendAction(message);
        }

        internal void JoinCommandHandler(Irc irc, string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            irc.JoinChannel(args[1]);
        }

        internal void PartCommandHandler(Irc irc, string[] args)
        {
            if (args.Length > 2)
            {
                return;
            }
            else if (args.Length == 2)
            {
                if (args[1].StartsWith("#"))
                    irc.PartChannel(args[1]);
            }
            else
            {
                irc.PartChannel(irc.currentChannel);
            }
        }

        internal void QuitCommandHandler(Irc irc, string[] args)
        {
            var message = String.Join(" ", args, 1, args.Length - 1);

            if (message != "")
            {
                irc.Disconnect(message);
            }
            else
            {
                irc.Disconnect();
            }
        }

        internal void HandleCommand(Irc irc, string text)
        {
            string[] args = text.Split(' ');
            if (args[0].StartsWith("//") || !args[0].StartsWith("/"))
            {
                if (args[0].StartsWith("//"))
                    args[0] = args[0].Replace("//", "/");
                irc.SendMessage(String.Join(" ", args));
            }
            else if (args[0].StartsWith("/"))
            {
                var cmd = CommandList.Where(command => command.StartsWith(args[0])).ToList();

                if (cmd.Count > 1)
                {
                    irc.ClientMessage("Multiple matches found: " + args[0]);
                    irc.ClientMessage(String.Join(", ", cmd));
                    irc.ClientMessage("Type /help for a list of commands.");

                    return;
                }
                else if (cmd.Count == 1)
                {
                    ((Command)CommandTable[cmd[0]])(irc, args);
                }
                else
                {
                    irc.ClientMessage("Unknown Command: " + text);
                    irc.ClientMessage("Type /help for a list of commands.");
                }
            }
        }

    }
}
