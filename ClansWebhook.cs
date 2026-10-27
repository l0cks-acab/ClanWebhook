using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ClansWebhook", "locks", "1.1.0")]
    [Description("Sends clan events to a Discord webhook and provides an admin command to post all clans")]
    class ClansWebhook : CovalencePlugin
    {
        [PluginReference] private Plugin Clans;

        private const string PermAdmin = "clanswebhook.admin";
        private const int ColorCreate = 0x2ECC71;
        private const int ColorDisband = 0xE74C3C;
        private const int ColorJoin = 0x3498DB;
        private const int ColorLeave = 0xE67E22;
        private const int ColorInfo = 0x95A5A6;

        private readonly Queue<string> queue = new Queue<string>();
        private Timer queueTimer;

        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Webhook URL")]
            public string WebhookUrl { get; set; } = "YOUR_DISCORD_WEBHOOK_URL_HERE";

            [JsonProperty("Footer Text")]
            public string FooterText { get; set; } = "developed by herbs.acab";

            [JsonProperty("Seconds Between Messages (rate limit)")]
            public float SendInterval { get; set; } = 1f;

            [JsonProperty("Events")]
            public EventSettings Events { get; set; } = new EventSettings();
        }

        public class EventSettings
        {
            [JsonProperty("Clan Created")]
            public bool Create { get; set; } = true;

            [JsonProperty("Clan Disbanded")]
            public bool Disband { get; set; } = true;

            [JsonProperty("Member Joined")]
            public bool Join { get; set; } = true;

            [JsonProperty("Member Left")]
            public bool Leave { get; set; } = true;

            [JsonProperty("Clan Updated (noisy)")]
            public bool Update { get; set; } = false;

            [JsonProperty("Clan Chat")]
            public bool Chat { get; set; } = false;
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new JsonException();
                if (config.Events == null) config.Events = new EventSettings();
                SaveConfig();
            }
            catch
            {
                PrintWarning("Config invalid, loading defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        #endregion

        #region Lifecycle

        private void Init() => permission.RegisterPermission(PermAdmin, this);

        private void OnServerInitialized()
        {
            if (Clans == null) PrintWarning("Clans plugin not loaded, clan info will be unavailable");
            queueTimer = timer.Every(Math.Max(0.5f, config.SendInterval), ProcessQueue);
        }

        private void Unload() => queueTimer?.Destroy();

        #endregion

        #region Hooks

        private void OnClanCreate(string tag)
        {
            if (!config.Events.Create) return;
            Send(ClanEmbed(tag, $"Clan Created: {tag}", ColorCreate));
        }

        private void OnClanUpdate(string tag)
        {
            if (!config.Events.Update) return;
            Send(ClanEmbed(tag, $"Clan Updated: {tag}", ColorInfo));
        }

        private void OnClanDisbanded(string tag, List<string> memberIds)
        {
            if (!config.Events.Disband) return;
            var members = memberIds != null && memberIds.Count > 0
                ? string.Join("\n", memberIds.Select(FormatPlayer))
                : "No members";
            Send(Embed($"Clan Disbanded: {tag}", $"**Members**\n{members}", ColorDisband));
        }

        private void OnClanMemberJoined(string userId, string tag)
        {
            if (!config.Events.Join) return;
            Send(Embed($"Player Joined Clan: {tag}", FormatPlayer(userId), ColorJoin));
        }

        private void OnClanMemberGone(string userId, string tag)
        {
            if (!config.Events.Leave) return;
            Send(Embed($"Player Left Clan: {tag}", FormatPlayer(userId), ColorLeave));
        }

        private void OnClanChat(IPlayer player, string message)
        {
            if (!config.Events.Chat || player == null) return;
            Send(Embed($"Clan Chat: {player.Name}", message, ColorInfo));
        }

        #endregion

        #region Commands

        [Command("postclans")]
        private void PostClansCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsServer && !player.IsAdmin && !player.HasPermission(PermAdmin))
            {
                player.Reply("You are not authorized to use this command.");
                return;
            }

            if (Clans == null)
            {
                player.Reply("Clans plugin not found.");
                return;
            }

            var tags = (Clans.Call("GetAllClans") as JArray)?.ToObject<List<string>>() ?? new List<string>();
            foreach (var tag in tags)
                Send(ClanEmbed(tag, $"Clan: {tag}", ColorInfo));

            player.Reply($"Queued {tags.Count} clan(s) for the webhook.");
        }

        #endregion

        #region Webhook

        private void Send(string payload)
        {
            if (payload == null) return;
            if (string.IsNullOrEmpty(config.WebhookUrl) || config.WebhookUrl == "YOUR_DISCORD_WEBHOOK_URL_HERE")
            {
                PrintWarning("Discord webhook URL is not set in the configuration.");
                return;
            }
            queue.Enqueue(payload);
        }

        private void ProcessQueue()
        {
            if (queue.Count == 0) return;
            var payload = queue.Dequeue();

            webrequest.Enqueue(config.WebhookUrl, payload, (code, response) =>
            {
                if (code == 429)
                {
                    // Rate limited: retry later
                    queue.Enqueue(payload);
                    return;
                }
                if (code < 200 || code >= 300)
                    PrintWarning($"Failed to send webhook: {code} - {response}");
            }, this, RequestMethod.POST, new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            });
        }

        #endregion

        #region Helpers

        private string Embed(string title, string description, int color)
        {
            return JsonConvert.SerializeObject(new
            {
                embeds = new[]
                {
                    new
                    {
                        title = Truncate(title, 256),
                        description = Truncate(description, 4096),
                        color,
                        timestamp = DateTime.UtcNow.ToString("o"),
                        footer = new { text = config.FooterText }
                    }
                }
            });
        }

        private string ClanEmbed(string tag, string title, int color)
        {
            var clan = Clans?.Call("GetClan", tag) as JObject;
            if (clan == null) return Embed(title, "Clan info unavailable", color);
            return Embed(title, $"**Members**\n{GetClanMembers(clan)}", color);
        }

        private string GetClanMembers(JObject clan)
        {
            var owner = clan["owner"]?.ToString();
            var lines = new List<string>();

            if (!string.IsNullOrEmpty(owner))
                lines.Add($"{FormatPlayer(owner)} (Owner)");

            var members = clan["members"] as JArray;
            if (members != null)
            {
                foreach (var id in members.Select(m => m.ToString()).Where(id => id != owner))
                    lines.Add(FormatPlayer(id));
            }

            return lines.Count > 0 ? string.Join("\n", lines) : "No members";
        }

        private string FormatPlayer(string id)
        {
            var player = covalence.Players.FindPlayerById(id);
            return player != null ? $"{player.Name} ({id})" : id;
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max - 3) + "...";

        #endregion
    }
}
