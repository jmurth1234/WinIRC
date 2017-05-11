# Changelog
-----

## 1.4.2

Fixes to a couple bugs found since the last update. Also attempts to fix a memory leak.

## 1.4.1
This is a bug fix update for the previous update that fixes the following issues:

 - Make channels case insensitive
 - Better error handling on connection issues 
 - Add a rate limiter on auto reconnects
 - Fix error when joining a server with the welcome page closed

This minor release also adds a /nick command to change usernames.

## 1.4.0

This release adds a major new feature - inline link viewing!

Certain links can now be viewed inline without leaving the IRC client, making viewing them much faster!

This is currently supported by:
 - Images (.png, .gif and .jpg)
 - Youtube videos
 - Twitter links

I've also added a JumpList, allowing you to connect to your saved servers by right clicking the taskbar icon or the entry in the start menu.

This release also further enhances the extended execution session for sessions that are even more extended than the last release!

Other changes in this release are:

 - Added auto reconnecting for when the app loses connection for whatever reason
 - Added the ability to ignore SSL cert errors
 - Added the ability to right click the "Server" entry in the list of channels to close and reconnect from the server
 - Fixed an issue with the list of users when joining a channel
 - Added handling for topic changes
 - Adds an _ to nicknames if there's a conflict
 - Fixed an issue with timestamps	
	
-----
	
## 1.3.1

This release of WinIRC features a major recode of the view of messages within a channel. Rather than just being a list of `<TextBlock>` elements, it's now a list of a custom `<MessageLine>` element. Seperate parts of the message can now be coloured and styled differently. Currently this brings two new features:

 - ping highlights in chat
 - timestamps in chat

This release also fixes a couple issues with the extended execution session on phones.

-----

## v1.3

This release adds an initial implementation of an ExtendedExecutionSession - this allows WinIRC to keep connected in the background. This has been tested to both work on a Lumia 435 running build 10586 and a desktop running build 14393. 

In both scenarios, the app stayed in the background for over 5 minutes without disconnecting, and notifications still showed up. However, on the phone it ceased being in the background once the phone was locked.

Other features and fixes in this release include:

 - A new option to automatically authenticate with nickserv once connecting to an irc server

 - Minor adjustments to icon assets to properly adhere to the Microsoft guidelines (should look better on HiDPI displays)

 - Fixes to tab behaviour - no new random server tabs when switching servers

 - Improvements and fixes to private messages - you'll get a desktop notification when you recieve one now, and the entry for the user messaged in the list of channels now displays their username.

Thanks for using WinIRC!

-----

## v1.2.19

Small patch to resolve an issue with the clean install experience

-----

## v1.2.18

I haven't made any progress on background connections yet, but until then have an update with a couple new features:

 - Tabs - Using the pivot control, you can now quickly switch between channels you've opened with tabs on the toolbar, or via swiping on a touchscreen! This can be disabled in the settings.

 - Shorter commands - you can now type shorter versions of commands, and WinIRC will match it to a command. If there's multiple possible matches, the command won't be executed

 - Application should not crash when IRC disconnects whilst suspended

-----

## v1.2.11

Minor release time! Nothing too big, just a couple of added features. The specific changes are as follows:

 - There's new buttons on the channel list for closing channels, and an easy way to join channels. 
 - There's also an option to hide or show the status bar on windows phone
 - I've also implemented an early form of /msg 

On background irc - this is planned for a future update. There's some promising new APIs coming in the anniversary update, which look like they'll allow me to easily put the app in the background. 

-----

## v1.2.7

The 1.2.7 bugfix fixes a number of issues with handing irc:// and ircs:// links when the app is closed. also fixes a crash bug relating to deleting servers.

-----

## v1.2.5 - Initial github release

This release of WinIRC adds a bunch of new features to the client. These features include:

 - New commands! Use /help to list them all when connected to a server
 - New channel moderation features!
 - Handles irc:// and ircs:// urls!
 - user list context menu!
 - on screen tab button for username completion on phones!
 - more polished top bar

There's also been some bug fixes, and performance enhancements!
