using System.Collections.Generic;
using TerrariaChatRelay;
using TerrariaChatRelay.Clients;

namespace TCRCQHttp
{
	public class Main : TCRPlugin
	{
		public static Configuration Config { get; set; }

		public override void Init(List<IChatClient> Subscribers)
		{
			Config = (Configuration)new Configuration().GetOrCreateConfiguration();

			if (Config.EnableCQHttp)
			{
				foreach (var cqhttpClient in Config.EndPoints)
					new ChatClient(Subscribers, cqhttpClient.BotApi, cqhttpClient.BotAccessToken, cqhttpClient.Group_IDs);
			}

			// not appropriate to have a ScanForCommands method in the interface but too lazy to think this out
			((CommandService)Core.CommandServ).ScanForCommands(this);
		}
	}
}
