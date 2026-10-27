# ClansWebhook

**ClansWebhook** is an Oxide plugin for Rust that sends clan events to a Discord webhook. Built for the [Clans](https://umod.org/plugins/clans) plugin by K1lly0u.

## Author
Plugin developed by **herbs.acab**

## Features

- Discord embed notifications (colored, timestamped) for:
  - Clan created
  - Clan disbanded (with member list)
  - Player joined a clan
  - Player left a clan
  - Clan updated (off by default, noisy)
  - Clan chat (off by default)
- Each event can be toggled in config
- Rate-limited send queue with automatic retry on Discord `429`
- `/postclans` posts every clan and its members to the webhook

## Installation

1. Place `ClansWebhook.cs` in `oxide/plugins`.
2. Start the server to generate the default config.
3. Edit `oxide/config/ClansWebhook.json` and reload the plugin.

## Permissions

- `clanswebhook.admin` — allows `/postclans` (server admins and console can always use it)

## Configuration

```json
{
  "Webhook URL": "YOUR_DISCORD_WEBHOOK_URL_HERE",
  "Footer Text": "developed by herbs.acab",
  "Seconds Between Messages (rate limit)": 1.0,
  "Events": {
    "Clan Created": true,
    "Clan Disbanded": true,
    "Member Joined": true,
    "Member Left": true,
    "Clan Updated (noisy)": false,
    "Clan Chat": false
  }
}
```
