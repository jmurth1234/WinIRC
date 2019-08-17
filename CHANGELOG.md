# Changelog

## 1.6.0

WinIRC 1.6 updates the SDK used to the fall creators update SDK, meaning you'll see some fluent design within the application! Specifically:

 - The sidebar has some acrylic blur applied to it
 - Fluent light effects are also present on the sidebar

In none fluent design related updates, there's a number of new features, including:

 - Link metadata previews! In chat you can now see previews of any links following the OpenGraph standard, allowing you see a thumbnail, the title and a brief description. This can be disabled in settings
 - Channel logging! This allows you to take logs of all your current channels and save it to a folder of your choice
 - You can now right click on a channel, and enable notifications for all messages to that channel.

Various other small changes and fixes include:
 - The twitter previewing is disabled on fall creators update devicesdue to a bug in the library used
 - Channels now only render the last 1k messages
 - Notifications are more reliable

This is quite possibly the last major release for a while - the next 2.0 release will be a near full recode. However, bugfixes will still be released for the 1.6 update for the foreseeable future.

-----

## 1.5.0

This release introduces a major overhaul of the UI, making the app more desktop friendly whilst still being touch friendly for mobiles and tablets! The major changes are:
 - The app now has a menu bar embedded in the title bar rather than a basic toolbar, where all of the previous buttons from the toolbar can be found
 - The topic is always showing now as a topic bar above the channel messages.
 - The list of channels on the right now includes all channels from all currently connected servers, grouped by server, rather than switching servers via a drop down
 
For new users, there's now a setup wizard that shows on the first launch, where users can enter a default username and select some sample servers to add to their server list. Existing users will be prompted on launch to set a default username.

Other improvements and fixes include:
 - Support for viewing more imgur links inline
 - Support for more server operator modes as used by some irc servers (contributed by TriJetScud)
 - Option to hide messages when a user joins or leaves a channel (conbtributed by owensdj)
 - Some small optimisations to joining ZNCs
 - Fixed out of control memory usage when connected to large amounts of channels

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


## v1.2.18

I haven't made any progress on background connections yet, but until then have an update with a couple new features:

 - Tabs - Using the pivot control, you can now quickly switch between channels you've opened with tabs on the toolbar, or via swiping on a touchscreen! This can be disabled in the settings.

 - Shorter commands - you can now type shorter versions of commands, and WinIRC will match it to a command. If there's multiple possible matches, the command won't be executed

 - Application should not crash when IRC disconnects whilst suspended

## v1.2.11

Minor release time! Nothing too big, just a couple of added features. The specific changes are as follows:

 - There's new buttons on the channel list for closing channels, and an easy way to join channels. 
 - There's also an option to hide or show the status bar on windows phone
 - I've also implemented an early form of /msg 

On background irc - this is planned for a future update. There's some promising new APIs coming in the anniversary update, which look like they'll allow me to easily put the app in the background. 

## v1.2.7

The 1.2.7 bugfix fixes a number of issues with handing irc:// and ircs:// links when the app is closed. also fixes a crash bug relating to deleting servers.

## v1.2.5 - Initial github release

This release of WinIRC adds a bunch of new features to the client. These features include:

 - New commands! Use /help to list them all when connected to a server
 - New channel moderation features!
 - Handles irc:// and ircs:// urls!
 - user list context menu!
 - on screen tab button for username completion on phones!
 - more polished top bar

There's also been some bug fixes, and performance enhancements!
