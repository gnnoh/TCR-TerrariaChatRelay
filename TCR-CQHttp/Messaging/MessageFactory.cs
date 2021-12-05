using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCRCQHttp.Models;

namespace TCRCQHttp
{
    public class CQHttpMessageFactory
    {
        /// <summary>
        /// Returns a JSON string that can be used to send a text message.
        /// </summary>
        /// <returns>JSON to send with.</returns>
        public static string CreateTextMessage(ulong groupId, string msg)
        {
            return "{\"action\": \"send_group_msg\", \"params\": {\"group_id\": " + groupId + ",\"message\": \"" + msg + "\",\"auto_escape\": true}}";
        }

        /// <summary>
        /// Attempts to convert the JSON string to a CQHttpMessage. Return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="json">JSON string to attempt to parse into a CQHttpMessage.</param>
        /// <returns>Equivalent CQHttpMessage</returns>
        public static bool TryParseMessage(string json, out Message.GroupMessage msg)
        {
            try
            {
                msg =  JsonConvert.DeserializeObject<Message.GroupMessage>(json);
                return true;
            }
            catch(JsonSerializationException)
            {
                msg = null;
                return false;
            }
        }
    }
}
