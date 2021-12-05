using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TCRCQHttp.Helpers
{
    public class WebSocketMessage
    {
        public JObject Content;
        public int MessageId;
        public WebSocketMessage(string content, int messageId)
        {
            Content = JObject.Parse(content);
            MessageId = messageId;
        }
    }
}
