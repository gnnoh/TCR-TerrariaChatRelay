using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay;
using TerrariaChatRelay.Command;
using Terraria.Localization;
using Terraria;

namespace TCRCQHttp.Commands
{
    [Command]
    class CmdPlaying : ICommand
    {
		public string Name { get; } = "Players Playing";

		public string CommandKey { get; } = "playing";

		public string Description { get; } = "List the players playing in the server.";

		public string Usage { get; } = "playing";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			// From Terraria Source Code
			string result = NetworkText.FromLiteral(string.Join(", ", ((IEnumerable<Player>)Terraria.Main.player).Where<Player>((Func<Player, bool>)(player => player.active)).Select<Player, string>((Func<Player, string>)(player => player.name))))._text;

			return result;
		}
	}
}
