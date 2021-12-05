using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay;
using TerrariaChatRelay.Helpers;

namespace TCRCQHttp
{
    public class Configuration : SimpleConfig<Configuration>
	{
		public override string FileName { get; set; }
			= Path.Combine(Global.ModConfigPath, "TerrariaChatRelay-CQHttp.json");

		[JsonProperty(Order = 5)]
		public string CommentGuide { get; set; } = "Setup Guide: https://tinyurl.com/TCR-Setup";
		[JsonProperty(Order = 10)]
		public bool EnableCQHttp { get; set; } = true;
		[JsonProperty(Order = 15)]
		public string CommandPrefix { get; set; } = "q!";
		[JsonProperty(Order = 20)]
		public bool ShowPoweredByMessageOnStartup { get; set; } = true;
		[JsonProperty(Order = 30)]
		public ulong OwnerUserId { get; set; } = 0;
		[JsonProperty(Order = 32)]
		public List<ulong> ManagerUserIds { get; set; } = new List<ulong>();
		[JsonProperty(Order = 33)]
		public List<ulong> AdminUserIds { get; set; } = new List<ulong>();
		[JsonProperty(Order = 34)]
		public List<Endpoint> EndPoints { get; set; } = new List<Endpoint>();

		[JsonProperty(Order = 35)]
		public string FormatHelp1 { get; set; } = "You can insert any of these formatters to change how your message looks! (CASE SENSITIVE)";
		[JsonProperty(Order = 40)]
		public string FormatHelp2 { get; set; } = "%playername% = Player Name";
		[JsonProperty(Order = 45)]
		public string FormatHelp3 { get; set; } = "%worldname% = World Name";
		[JsonProperty(Order = 50)]
		public string FormatHelp4 { get; set; } = "%message% = Initial message content";
		[JsonProperty(Order = 55)]
		public string FormatHelp5 { get; set; } = "%bossname% = Name of boss being summoned (only for VanillaBossSpawned)";
		[JsonProperty(Order = 60)]
		public string FormatHelp6 { get; set; } = "%groupprefix% = Group prefix";
		[JsonProperty(Order = 65)]
		public string FormatHelp7 { get; set; } = "%groupsuffix% = Group suffix";

		[JsonProperty(Order = 70)]
		public static string PlayerChatFormat = "%playername%: %message%";
		[JsonProperty(Order = 80)]
		public static string PlayerLoggedInFormat = "%playername% joined the server.";
		[JsonProperty(Order = 90)]
		public static string PlayerLoggedOutFormat = "%playername% left the server.";
		[JsonProperty(Order = 100)]
		public static string WorldEventFormat = "%message%";
		[JsonProperty(Order = 105)]
		public static string ServerStartingFormat = "%message%";
		[JsonProperty(Order = 110)]
		public static string ServerStoppingFormat = "%message%";
		[JsonProperty(Order = 115)]
		public static string VanillaBossSpawned = "%bossname% has awoken!";

		public Configuration()
		{
			if (!File.Exists(FileName))
			{
				// CQHttp
				EndPoints.Add(new Endpoint());
				ManagerUserIds.Add(0);
				AdminUserIds.Add(0);
			}
		}
	}

    public class Endpoint
	{
		public string BotApi { get; set; } = "wss://example.com/cqhttp";
        public string BotAccessToken { get; set; } = "BOT_ACCESS_TOKEN";
        public ulong[] Group_IDs { get; set; } = { 0 };
    }
}
