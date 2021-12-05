using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TerrariaChatRelay;
using TerrariaChatRelay.Command;

namespace TCRCQHttp.Commands
{
	[Command]
	public class CmdRemoveAdmin : ICommand
	{
		public string Name { get; } = "Remove Admin";

		public string CommandKey { get; } = "removeadmin";

		public string Description { get; } = "Removes the user's access to Administrator level commands.";

		public string Usage { get; } = "removeadmin @QQUser";

		public Permission DefaultPermissionLevel { get; } = Permission.Owner;

		readonly Regex cqCodeAtFinder = new Regex(@"\[CQ:at,qq=[0-9al]*\]");

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			var match = cqCodeAtFinder.Match(input);
			if (match.Success)
			{
				string qqNumber = match.Value.Substring(10).Substring(0, match.Value.Length - 1);
				input = input.Replace(match.Value, qqNumber);
			}

			if (ulong.TryParse(input, out ulong userId))
			{
				if (Main.Config.AdminUserIds.Contains(userId))
				{
					Main.Config.AdminUserIds.Remove(userId);
					Main.Config.SaveJson();
					return "User successfully deleted.";
				}
				else
				{
					return "Could not find user in admin database.";
				}
			}
			else
			{
				return "Could not find user. Example: removeadmin @UserToRemovePermissions";
			}
		}
	}
}
