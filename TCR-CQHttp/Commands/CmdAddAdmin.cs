using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TerrariaChatRelay;
using TerrariaChatRelay.Command;
using TerrariaChatRelay.Helpers;

namespace TCRCQHttp.Commands
{
	[Command]
	public class CmdAddAdmin : ICommand
	{
		public string Name { get; } = "Add Admin";

		public string CommandKey { get; } = "addadmin";

		public string Description { get; } = "Grants the user access to Administrator level commands.";

		public string Usage { get; } = "addadmin @QQUser";

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
				Main.Config.AdminUserIds.Add(userId);
				Main.Config.SaveJson();
				return "User successfully added.";
			}
			else
			{
				return "Could not find user. Example: addadmin @UserToGivePermissions " + input;
			}
		}
	}
}
