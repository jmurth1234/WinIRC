# What is WinIRC?

WinIRC is a C# IRC Client for Windows 10. It's written as a UWP application, so don't expect this to be ported to Windows 8.1 or below.

## What features does it have?

* Joining onto irc servers, both ZNCs and standard irc servers
* Supports a direct irc connection and connection via a websocket connection (this is hosted on your own server to get around any pesky filters)
* Join and parting channels
* /help to list supported commands
* Various irc commands, including /query and /whois
* Basic channel moderation features (/mode, /kick, /ban, /mute, ect.)
* A few configuration options
* Responsive UI

## How do I get it?

For autoupdating, the easiest way to get it is from the [Microsoft Store](https://www.microsoft.com/en-us/store/apps/winirc/9nblggh2p0rf). 

If you downloaded the sideloadable package, make sure you enabled developer mode in `Settings -> Update & Security -> For developers -> Developer mode`. Once this is done, on the latest insider builds and on Windows 10 phones you should be able to double click the appxbundle to install it.

## Building from source

You'll need the following:

* Windows 10 Anniversary Update or later
* Viual Studio 2017 

Once you've downloaded the project (either via a git clone, the "Open in Visual Studio" button in github, or via some other method) and opened in visual studio the compilation steps are as follows:

1. Clean solution
2. Set the properties of ApplicationInsights.config to "Build Action: None" and "Copy to output Directory: Do not copy"
3. Ensure you have a certificate for deploying the app - 
    * Go to `Package.appmanifest -> Packaging -> Choose Certificate`
	* In the dialog, click `Configure Certificate -> Create Test Certificate`
	* Keep the default settings, then click ok and ok again
4. Build solution
5. Using the top toolbar, select the architecture and the device you want to deploy the app on
6. And that should be it!

## Contributing

Contributions are welcome, so feel free to make changes and submit a pull request!

There's no real code style guidelines, just make sure the code is properly indented and has the same brackets layout as the rest of the application. 
