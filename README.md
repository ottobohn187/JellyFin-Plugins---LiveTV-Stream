# Jellyfin Plugins

Workspace for the Jellyfin LiveTV Stream plugins and related development notes.

## Jellyfin Repository URL

Add this URL in Jellyfin under `Dashboard -> Plugins -> Repositories`:

`https://raw.githubusercontent.com/ottobohn187/JellyFin-Plugins---LiveTV-Stream/main/manifest.json`

Jellyfin can read that manifest and install the packaged plugin directly from GitHub.

The repository contains FOX43 Stream, ABC27 Stream, and Release Watcher. The publish workflow packages all three into the same Jellyfin repository feed.
