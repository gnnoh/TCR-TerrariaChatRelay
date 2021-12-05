using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCRCQHttp.Models
{
    public class Message
    {
        public class GroupMessage
        {
            [JsonProperty("time")]
            public ulong TimeStamp { get; set; }
            [JsonProperty("self_id")]
            public ulong SelfId { set; get; }
            [JsonProperty("post_type")]
            public string Type { set; get; }
            [JsonProperty("message_type")]
            public string MessageType { set; get; }
            [JsonProperty("sub_type")]
            public string SubType { get; set; }
            [JsonProperty("message_id")]
            public long MessageId { get; set; }
            [JsonProperty("group_id")]
            public ulong GroupId { get; set; }
            [JsonProperty("user_id")]
            public ulong UserId { get; set; }
            [JsonProperty("anonymous")]
            public MessageData.Anonymous AnonymousUser { get; set; }
            [JsonProperty("message")]
            public string MessageData { get; set; }
            [JsonProperty("raw_message")]
            public string RawMessage { get; set; }
            [JsonProperty("font")]
            public ulong Font { get; set; }
            [JsonProperty("sender")]
            public MessageData.Sender Author { get; set; }
        }
        public class GroupMemberInfo
        {
            [JsonProperty("group_id")]
            public ulong GroupId { get; set; }
            [JsonProperty("user_id")]
            public ulong UserId { get; set; }
            [JsonProperty("nickname")]
            public string Nickname { get; set; }
            [JsonProperty("card")]
            public string Card { get; set; }
            [JsonProperty("sex")]
            public string Sex { get; set; }
            [JsonProperty("age")]
            public int Age { get; set; }
            [JsonProperty("area")]
            public string Area { get; set; }
            [JsonProperty("join_time")]
            public ulong JoinTime { get; set; }
            [JsonProperty("last_sent_time")]
            public ulong LastSentTime { get; set; }
            [JsonProperty("level")]
            public string Level { get; set; }
            [JsonProperty("role")]
            public string Role { get; set; }
            [JsonProperty("unfriendly")]
            public bool IsUnfriendly { get; set; }
            [JsonProperty("title")]
            public string Title { get; set; }
            [JsonProperty("title_expire_time")]
            public ulong TitleExpireTime { get; set; }
            [JsonProperty("card_changeable")]
            public bool IsCardChangeable { get; set; }
        }
    }
}
