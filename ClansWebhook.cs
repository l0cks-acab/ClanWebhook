using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("ClansWebhook", "locks", "1.0.0")]
    [Description("Sends clan events to a Discord webhook")]

    class ClansWebhook : CovalencePlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Webhook URL")]
            public string WebhookUrl { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new Configuration
            {
                WebhookUrl = "YOUR_DISCORD_WEBHOOK_URL_HERE"
            }, true);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
        }

        #endregion

        #region Hooks

        private void OnClanCreate(string clanTag, string clanName, IPlayer owner)
        {
            string message = $"Clan Created: {clanTag}\nOwner: {owner.Name} ({owner.Id})\nTime: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}";
            SendWebhook(message);
        }

        private void OnClanDisband(string clanTag)
        {
            string message = $"Clan Disbanded: {clanTag}\nTime: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}";
            SendWebhook(message);
        }

        private void OnClanMemberJoined(string clanTag, IPlayer player)
        {
            string message = $"Player Joined Clan: {clanTag}\nPlayer: {player.Name} ({player.Id})\nTime: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}";
            SendWebhook(message);
        }

        private void OnClanMemberGone(string clanTag, IPlayer player)
        {
            string message = $"Player Left Clan: {clanTag}\nPlayer: {player.Name} ({player.Id})\nTime: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}";
            SendWebhook(message);
        }

        #endregion

        #region Webhook

        private void SendWebhook(string message)
        {
            if (string.IsNullOrEmpty(config.WebhookUrl))
            {
                Puts("Discord webhook URL is not set in the configuration.");
                return;
            }

            webrequest.Enqueue(config.WebhookUrl, JsonConvert.SerializeObject(new
            {
                content = message
            }), (code, response) =>
            {
                if (code != 204)
                {
                    Puts($"Failed to send webhook: {code} - {response}");
                }
            }, this, RequestMethod.POST, new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            });
        }

        #endregion
    }
}
