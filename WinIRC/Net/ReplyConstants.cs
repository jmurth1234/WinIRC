using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIRC.Net
{
    class ReplyConstants
    {
        // Error Replies.
        public static int ERR_NOSUCHNICK = 401;
        public static int ERR_NOSUCHSERVER = 402;
        public static int ERR_NOSUCHCHANNEL = 403;
        public static int ERR_CANNOTSENDTOCHAN = 404;
        public static int ERR_TOOMANYCHANNELS = 405;
        public static int ERR_WASNOSUCHNICK = 406;
        public static int ERR_TOOMANYTARGETS = 407;
        public static int ERR_NOORIGIN = 409;
        public static int ERR_NORECIPIENT = 411;
        public static int ERR_NOTEXTTOSEND = 412;
        public static int ERR_NOTOPLEVEL = 413;
        public static int ERR_WILDTOPLEVEL = 414;
        public static int ERR_UNKNOWNCOMMAND = 421;
        public static int ERR_NOMOTD = 422;
        public static int ERR_NOADMININFO = 423;
        public static int ERR_FILEERROR = 424;
        public static int ERR_NONICKNAMEGIVEN = 431;
        public static int ERR_ERRONEUSNICKNAME = 432;
        public static int ERR_NICKNAMEINUSE = 433;
        public static int ERR_NICKCOLLISION = 436;
        public static int ERR_USERNOTINCHANNEL = 441;
        public static int ERR_NOTONCHANNEL = 442;
        public static int ERR_USERONCHANNEL = 443;
        public static int ERR_NOLOGIN = 444;
        public static int ERR_SUMMONDISABLED = 445;
        public static int ERR_USERSDISABLED = 446;
        public static int ERR_NOTREGISTERED = 451;
        public static int ERR_NEEDMOREPARAMS = 461;
        public static int ERR_ALREADYREGISTRED = 462;
        public static int ERR_NOPERMFORHOST = 463;
        public static int ERR_PASSWDMISMATCH = 464;
        public static int ERR_YOUREBANNEDCREEP = 465;
        public static int ERR_KEYSET = 467;
        public static int ERR_CHANNELISFULL = 471;
        public static int ERR_UNKNOWNMODE = 472;
        public static int ERR_INVITEONLYCHAN = 473;
        public static int ERR_BANNEDFROMCHAN = 474;
        public static int ERR_BADCHANNELKEY = 475;
        public static int ERR_NOPRIVILEGES = 481;
        public static int ERR_CHANOPRIVSNEEDED = 482;
        public static int ERR_CANTKILLSERVER = 483;
        public static int ERR_NOOPERHOST = 491;
        public static int ERR_UMODEUNKNOWNFLAG = 501;
        public static int ERR_USERSDONTMATCH = 502;
        // Command Responses.
        public static int RPL_TRACELINK = 200;
        public static int RPL_TRACECONNECTING = 201;
        public static int RPL_TRACEHANDSHAKE = 202;
        public static int RPL_TRACEUNKNOWN = 203;
        public static int RPL_TRACEOPERATOR = 204;
        public static int RPL_TRACEUSER = 205;
        public static int RPL_TRACESERVER = 206;
        public static int RPL_TRACENEWTYPE = 208;
        public static int RPL_STATSLINKINFO = 211;
        public static int RPL_STATSCOMMANDS = 212;
        public static int RPL_STATSCLINE = 213;
        public static int RPL_STATSNLINE = 214;
        public static int RPL_STATSILINE = 215;
        public static int RPL_STATSKLINE = 216;
        public static int RPL_STATSYLINE = 218;
        public static int RPL_ENDOFSTATS = 219;
        public static int RPL_UMODEIS = 221;
        public static int RPL_STATSLLINE = 241;
        public static int RPL_STATSUPTIME = 242;
        public static int RPL_STATSOLINE = 243;
        public static int RPL_STATSHLINE = 244;
        public static int RPL_LUSERCLIENT = 251;
        public static int RPL_LUSEROP = 252;
        public static int RPL_LUSERUNKNOWN = 253;
        public static int RPL_LUSERCHANNELS = 254;
        public static int RPL_LUSERME = 255;
        public static int RPL_ADMINME = 256;
        public static int RPL_ADMINLOC1 = 257;
        public static int RPL_ADMINLOC2 = 258;
        public static int RPL_ADMINEMAIL = 259;
        public static int RPL_TRACELOG = 261;
        public static int RPL_NONE = 300;
        public static int RPL_AWAY = 301;
        public static int RPL_USERHOST = 302;
        public static int RPL_ISON = 303;
        public static int RPL_UNAWAY = 305;
        public static int RPL_NOWAWAY = 306;
        public static int RPL_WHOISUSER = 311;
        public static int RPL_WHOISSERVER = 312;
        public static int RPL_WHOISOPERATOR = 313;
        public static int RPL_WHOWASUSER = 314;
        public static int RPL_ENDOFWHO = 315;
        public static int RPL_WHOISIDLE = 317;
        public static int RPL_ENDOFWHOIS = 318;
        public static int RPL_WHOISCHANNELS = 319;
        public static int RPL_LISTSTART = 321;
        public static int RPL_LIST = 322;
        public static int RPL_LISTEND = 323;
        public static int RPL_CHANNELMODEIS = 324;
        public static int RPL_NOTOPIC = 331;
        public static int RPL_TOPIC = 332;
        public static int RPL_TOPICINFO = 333;
        public static int RPL_INVITING = 341;
        public static int RPL_SUMMONING = 342;
        public static int RPL_VERSION = 351;
        public static int RPL_WHOREPLY = 352;
        public static int RPL_NAMREPLY = 353;
        public static int RPL_LINKS = 364;
        public static int RPL_ENDOFLINKS = 365;
        public static int RPL_ENDOFNAMES = 366;
        public static int RPL_BANLIST = 367;
        public static int RPL_ENDOFBANLIST = 368;
        public static int RPL_ENDOFWHOWAS = 369;
        public static int RPL_INFO = 371;
        public static int RPL_MOTD = 372;
        public static int RPL_ENDOFINFO = 374;
        public static int RPL_MOTDSTART = 375;
        public static int RPL_ENDOFMOTD = 376;
        public static int RPL_YOUREOPER = 381;
        public static int RPL_REHASHING = 382;
        public static int RPL_TIME = 391;
        public static int RPL_USERSSTART = 392;
        public static int RPL_USERS = 393;
        public static int RPL_ENDOFUSERS = 394;
        public static int RPL_NOUSERS = 395;
        // Reserved Numerics.
        public static int RPL_TRACECLASS = 209;
        public static int RPL_STATSQLINE = 217;
        public static int RPL_SERVICEINFO = 231;
        public static int RPL_ENDOFSERVICES = 232;
        public static int RPL_SERVICE = 233;
        public static int RPL_SERVLIST = 234;
        public static int RPL_SERVLISTEND = 235;
        public static int RPL_WHOISCHANOP = 316;
        public static int RPL_KILLDONE = 361;
        public static int RPL_CLOSING = 362;
        public static int RPL_CLOSEEND = 363;
        public static int RPL_INFOSTART = 373;
        public static int RPL_MYPORTIS = 384;
        public static int ERR_YOUWILLBEBANNED = 466;
        public static int ERR_BADCHANMASK = 476;
        public static int ERR_NOSERVICEHOST = 492;

        /**
         * Should not be initialized.
         */
        private ReplyConstants()
        {
        }
    }
}
