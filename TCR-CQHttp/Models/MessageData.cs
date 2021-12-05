using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCRCQHttp.Models
{
	public class MessageData
	{
		public class Anonymous
		{
			[JsonProperty("flag")]
			public string AnonymousFlag { get; set; }
			[JsonProperty("id")]
			public string AnonymousId { get; set; }
			[JsonProperty("name")]
			public string AnonymousName { get; set; }

		}
		public class Sender
		{
			[JsonProperty("user_id")]
			public ulong SenderId { get; set; }
			[JsonProperty("nickname")]
			public string SenderNickname { get; set; }
			[JsonProperty("card")]
			public string SenderCard { get; set; } = null;
			[JsonProperty("sex")]
			public string SenderSex { get; set; }
			[JsonProperty("age")]
			public int SenderAge { get; set; }
			[JsonProperty("area")]
			public string SenderArea { get; set; }
			[JsonProperty("level")]
			public string SenderLevel { get; set; }
			[JsonProperty("role")]
			public string SenderRole { get; set; } = "member";
			[JsonProperty("title")]
			public string SenderTitle { get; set; } = null;
		}
	}
}
