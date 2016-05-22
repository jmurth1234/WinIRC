using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WinIRC.Net
{
    /// <summary>
    /// Breaks an IRC server response down in to a prefix, command w/ parameters and an optional trail.
    /// Since the response format is always :<prefix> <Command> <arg, arg, arg ...> :trailing content
    /// the Parser can easily break the response down in to objects representing each component of
    /// the message.
    /// 
    /// Code found on stackexchange - http://codereview.stackexchange.com/questions/78713/irc-server-response-parser-for-irc-client
    /// </summary>
    public class IrcMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IrcMessage"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public IrcMessage(string message)
        {
            // remove colours first
            var coloursRegex = new Regex(@"[\x02\x1f\x16\x0f]|\x03\d{0,2}(?:,\d{0,2})?");
            message = coloursRegex.Replace(message, "");

            // split the line into an array
            var lineSplit = message.Split(' ').ToList();

            // get the server time, if it's there
            if (message.StartsWith("@"))
            {
                var timeVar = lineSplit[0];
                this.ServerTime = timeVar.Replace("@time=", "");
                lineSplit.RemoveAt(0);
            }

            message = String.Join(" ", lineSplit);

            this.OriginalMessage = message;
            this.PrefixMessage = new MessagePrefix(message);
            this.TrailMessage = new MessageTrail(message);
            this.CommandMessage = new MessageCommand(message, this.PrefixMessage, this.TrailMessage);
        }

        /// <summary>
        /// Gets the original message sent from the server.
        /// </summary>
        public string OriginalMessage { get; private set; }

        /// <summary>
        /// Gets the prefix of the response.
        /// </summary>
        public MessagePrefix PrefixMessage { get; private set; }

        /// <summary>
        /// Gets the trailing message. Not all responses come with trailing content.
        /// </summary>
        public MessageTrail TrailMessage { get; private set; }

        /// <summary>
        /// Gets the command and its parameters sent from the server.
        /// A command will always be given; parameters might be empty.
        /// </summary>
        public MessageCommand CommandMessage { get; private set; }

        /// <summary>
        /// Gets the time that might be sent from the server.
        /// This could be empty.
        /// </summary>
        public string ServerTime { get; private set; }
    }

    /// <summary>
    /// Parses a server response for a prefix and stores it.
    /// </summary>
    public class MessagePrefix
    {
        /// <summary>
        /// The original message
        /// </summary>
        private string originalMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePrefix"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public MessagePrefix(string message)
        {
            this.originalMessage = message;

            if (!this.IsPrefixed)
            {
                return;
            }

            // Prefix always starts with a colon and ends with a space.
            // :<Prefix> <Command>
            this.EndIndex = message.IndexOf(" ");
            this.Prefix = message.Substring(1, this.EndIndex - 1);

            var userHostArray = this.Prefix.Split(new string[] { "!", "@" }, StringSplitOptions.None);

            if (userHostArray.Length > 1)
            {
                this.IsUser = true;
                this.Nickname = userHostArray[0];
                this.Username= userHostArray[1];
                this.Hostname = userHostArray[2];
            }
        }

        /// <summary>
        /// Gets a value indicating whether the original message contained a properly formatted prefixed.
        /// </summary>
        public bool IsPrefixed
        {
            get
            {
                return this.originalMessage.StartsWith(":");
            }
        }

        /// <summary>
        /// Gets the index at the end of the prefix.
        /// </summary>
        public int EndIndex { get; private set; }

        /// <summary>
        /// Gets the prefix of the message. This is all text after the initial colon and before the first space.
        /// </summary>
        public string Prefix { get; private set; }
        public bool IsUser { get; private set; }
        public string Nickname { get; private set; }
        public string Username { get; private set; }
        public string Hostname { get; private set; }
    }

    /// <summary>
    /// Parses an IRC server response for the command and its arguments.
    /// </summary>
    public class MessageCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCommand"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="trail">The trail.</param>
        public MessageCommand(string message, MessagePrefix prefix, MessageTrail trail)
        {
            // Remove all of the content after the Prefix but before the Trail.
            // That is our Command and it's associated arguments.
            // Formatted as
            // :<Prefix> <Command> <space separated args> :<Trail>
            string[] commandAndParameters = message
                .Substring(prefix.EndIndex + 1, trail.TrailStart - prefix.EndIndex - 1)
                .Split(' ');

            // First item is always the command.
            this.Command = commandAndParameters[0];

            // If the command as args, then they compose the rest of the collection.
            if (commandAndParameters.Length > 1)
            {
                this.Parameters = commandAndParameters.Skip(1).ToList();
            }
        }

        /// <summary>
        /// Gets the command sent by the server.
        /// </summary>
        public string Command { get; private set; }

        /// <summary>
        /// Gets the parameters associated with the command, if any..
        /// </summary>
        public List<string> Parameters { get; private set; }
    }

    /// <summary>
    /// Parses an IRC server response for trailing content.
    /// </summary>
    public class MessageTrail
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageTrail"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public MessageTrail(string message)
        {
            string r = "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
            message = Regex.Replace(message, r, "", RegexOptions.Compiled);

            // Find the index of the closing colon used to mark the end of the 
            // message meta and start of the trailing content.
            // This is always the first space, followed by a colon in the response.
            this.TrailStart = message.IndexOf(" :");
            if (this.TrailStart >= 0)
            {
                // Set the trailing content to the location were the trailing content begins.
                // The +2 is needed to begin after the ' :' index.
                this.TrailingContent = message.Substring(this.TrailStart + 2);
            }
            else
            {
                this.TrailStart = message.Length;
            }

            this.HasTrail = this.TrailStart > 0 && this.TrailStart < message.Length;
        }

        /// <summary>
        /// Gets a value indicating whether the server response has trailing content.
        /// </summary>
        public bool HasTrail { get; private set; }

        /// <summary>
        /// Gets the trailing content of the response.
        /// </summary>
        public string TrailingContent { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the string index from the original message where the trailing content starts.
        /// </summary>
        public int TrailStart { get; private set; }
    }
}
