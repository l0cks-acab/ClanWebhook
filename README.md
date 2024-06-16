# ClansWebhook

**ClansWebhook** is an Oxide plugin for Rust that sends announcements to a Discord webhook whenever there is a clan-related event, such as creating or disbanding a clan, or adding or removing a player from a clan. Intended for use with the clan plugin by K1lly0u [Link](https://umod.org/plugins/clans)

## Features

- Sends notifications to a Discord webhook for the following events:
  - Clan creation
  - Clan disbandment
  - Player joining a clan
  - Player leaving a clan
  - Provides an admin command to post all clans and their members to the webhook (/postclans).

## Installation

1. Download the `ClansWebhook.cs` file and place it in your `oxide/plugins` directory.
2. Start your Rust server to generate the default configuration file.
3. Configure the plugin by editing the `ClansWebhook.json` file in your `oxide/config` directory.

## Configuration

The configuration file (`oxide/config/ClansWebhook.json`) will look like this:

```json
{
  "Webhook URL": "YOUR_DISCORD_WEBHOOK_URL_HERE"
}
```
