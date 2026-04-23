# ABC27 Stream Jellyfin Plugin

This Jellyfin plugin targets Jellyfin Server 10.11.x. It adds:

- a plugin configuration page
- `/Plugins/Abc27Stream/playlist.m3u`
- `/Plugins/Abc27Stream/stream`
- `/Plugins/Abc27Stream/status`
- `/Plugins/Abc27Stream/icon.jpg`

The default configuration uses the ABC27 Anvato player metadata found in the saved ABC27 watch-page HTML supplied by the user. The plugin resolves a fresh HLS manifest on demand so the generated playlist stays usable even when the upstream stream tokens rotate.

## Build

Install .NET SDK 9.0, then from this folder run:

```powershell
cd Jellyfin.Plugin.Abc27Stream
dotnet restore
dotnet publish -c Release
```

## Install manually

Copy the published files from:

```text
Jellyfin.Plugin.Abc27Stream/bin/Release/net9.0/publish/
```

into a folder such as:

```text
<jellyfin data dir>/plugins/Abc27Stream/
```

Restart Jellyfin.

## Use

After install, open Dashboard -> My Plugins -> ABC27 Stream.

Then use the generated playlist URL as an M3U source in Jellyfin Live TV, Threadfin, or xTeVe.

## Install From GitHub Repository

In Jellyfin, open `Dashboard -> Plugins -> Repositories` and add:

`https://raw.githubusercontent.com/ottobohn187/JellyFin-Plugins---LiveTV-Stream/main/manifest.json`

The published feed in this repository includes the packaged plugin zip and manifest for installation through Jellyfin's plugin repositories.
