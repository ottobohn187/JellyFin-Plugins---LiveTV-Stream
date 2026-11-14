# Release Watcher

Release Watcher checks the official `jellyfin/jellyfin` GitHub releases at server startup and every 24 hours. When a newer version is found, it creates one administrator activity notification for that version.

The plugin is notification-only. It never downloads or installs Jellyfin updates.

The check can also be run manually from the plugin settings page or from **Dashboard > Scheduled Tasks > Application > Check for Jellyfin updates**.
