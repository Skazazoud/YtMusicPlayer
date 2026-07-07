# YT Music Player

A lightweight Windows desktop app for YouTube Music, built with WinForms and WebView2.
It wraps music.youtube.com in its own window instead of a browser tab, so it stays out
of your way: it lives in the system tray, works with your keyboard's media keys, shows
up in Windows' Now Playing controls, and remembers your window position and sign-in
between launches.

This is an unofficial, personal project and isn't affiliated with Google or YouTube.

## Installing

1. Get `YtMusicPlayerSetup.msi` from whoever sent it to you.
2. Double-click it and follow the install wizard (you can change the install
   location if you want, otherwise the default is fine).
3. When it finishes, you'll have a **YT Music Player** shortcut on your Desktop
   and in the Start Menu.

No other setup needed — the installer bundles everything the app needs to run,
including the .NET runtime. The only thing it relies on that isn't bundled is the
Microsoft Edge WebView2 Runtime, which is already installed on virtually every
Windows 10/11 PC (it ships with Edge). If it's somehow missing, the app will tell
you and open the download page for you instead of just crashing.

The install is per-user, so it doesn't need administrator rights.

### First launch

Open the app and sign in with your Google account from the YouTube Music page
that loads (top-right corner). If you have YouTube Premium, you'll get ad-free
and background playback like you would in a browser. Your sign-in is remembered,
so you only need to do this once.

## Using it

- **Closing the window** (the X button) doesn't quit the app — it minimizes it to
  the system tray so your music keeps playing. Look for its icon near the clock.
- **Left-click the tray icon** to bring the window back; **right-click** it for
  options, including **Exit** (which actually quits the app) and **Run when my
  computer starts**.
- Your keyboard's media keys (play/pause, next, previous) and Windows' Now
  Playing widget (in the volume flyout or lock screen) control playback even
  while the window is hidden.

## Uninstalling

Search for **YT Music Player** in *Settings → Apps* (or *Add or Remove Programs*)
and uninstall it like any other app.

## Building it yourself

If you have the source and the .NET SDK installed:

```
dotnet publish -c Release -r win-x64 --self-contained true -o publish
dotnet wix build setup/Product.wxs -ext WixToolset.UI.wixext -o setup/YtMusicPlayerSetup.msi
```

The first command produces a self-contained build in `publish/`; the second
packages it into the installer using the WiX toolset (installed automatically
as a local `dotnet tool` the first time you run `dotnet tool restore`).
