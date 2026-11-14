# Jellyfin Plugins

Workspace for the Jellyfin LiveTV Stream plugins and related development notes.

## Jellyfin Repository URL

Add this URL in Jellyfin under `Dashboard -> Plugins -> Repositories`:

`https://raw.githubusercontent.com/ottobohn187/JellyFin-Plugins---LiveTV-Stream/main/manifest.json`

Jellyfin can read that manifest and install the packaged plugin directly from GitHub.

The repository contains FOX43 Stream, ABC27 Stream, Release Watcher, and Music
Video Visualizer. The publish workflow packages all four into the same Jellyfin
repository feed.

Music Video Visualizer can be limited to a designated library such as `Music
Videos`. Its rainbow spectrum overlay currently supports Jellyfin Web clients.
