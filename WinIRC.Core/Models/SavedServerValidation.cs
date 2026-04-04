using System;

namespace WinIRC.Core.Models;

public static class SavedServerValidation
{
    public static bool TryValidateAndNormalize(SavedServer server, out string errorMessage)
    {
        errorMessage = string.Empty;

        server.Name = (server.Name ?? string.Empty).Trim();
        server.Hostname = (server.Hostname ?? string.Empty).Trim();
        server.Username = (server.Username ?? string.Empty).Trim();
        server.Password = server.Password ?? string.Empty;
        server.NickservPassword = server.NickservPassword ?? string.Empty;
        server.Channels = (server.Channels ?? string.Empty).Trim();

        if (server.Hostname.Length == 0)
        {
            errorMessage = "No hostname entered.";
            return false;
        }

        if (Uri.CheckHostName(server.Hostname) == UriHostNameType.Unknown)
        {
            errorMessage = "Hostname is incorrect.";
            return false;
        }

        if (server.Port <= 0)
        {
            errorMessage = "Port must be greater than zero.";
            return false;
        }

        if (server.Username.Length == 0)
        {
            errorMessage = "No username entered.";
            return false;
        }

        if (server.Username.Contains(" "))
        {
            errorMessage = "Usernames cannot contain spaces.";
            return false;
        }

        if (server.Name.Length == 0)
        {
            server.Name = server.Hostname;
        }

        return true;
    }
}
