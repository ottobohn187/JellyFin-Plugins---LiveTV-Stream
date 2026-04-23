# FOX43 Stream Jellyfin Plugin (demo)

This is a demo Jellyfin plugin for Jellyfin Server 10.11.8. It adds:

- a plugin configuration page
- `/Plugins/Fox43Stream/playlist.m3u`
- `/Plugins/Fox43Stream/stream`
- `/Plugins/Fox43Stream/status`

The default stream URL is the FOX43 HLS URL found in the saved FOX43 watch-page HTML supplied by the user.

## Build

Install .NET SDK 9.0, then from this folder run:

```powershell
cd Jellyfin.Plugin.Fox43Stream
dotnet restore
dotnet publish -c Release
```

## Install manually

Copy the published files from:

```text
Jellyfin.Plugin.Fox43Stream/bin/Release/net8.0/publish/
```

into a folder such as:

```text
<jellyfin data dir>/plugins/Fox43Stream/
```

Restart Jellyfin.

## Use

After install, open Dashboard -> My Plugins -> FOX43 Stream.

Then use the generated playlist URL as an M3U source in Jellyfin Live TV, Threadfin, or xTeVe.
