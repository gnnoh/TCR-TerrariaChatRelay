using TCRCQHttp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebSocketSharp;

namespace TCRCQHttp.Helpers
{
    public class ChatParser
    {
        Regex cqCodeFinder { get; }
        Regex cqCodeParamFinder { get; }
        Regex colorCodeFinder { get; }
        Regex itemCodeFinder { get; }

        public ChatParser()
        {
            colorCodeFinder = new Regex(@"\[c\/.*?:(.*?)\]");
            itemCodeFinder = new Regex(@"\[i:(.*?)\]");
        }

        public string UnEscapeCQHttpRawMessage(string originalMessage)
        {
            originalMessage = originalMessage.Replace("&amp", "&");
            originalMessage = originalMessage.Replace("&#91", "[");
            originalMessage = originalMessage.Replace("&#93", "]");

            return originalMessage;
        }

        public string RemoveTerrariaColorAndItemCodes(string chatMessage)
		{
            var match = colorCodeFinder.Match(chatMessage);

            while (match.Success)
            {
                if (match.Groups.Count >= 2)
                    chatMessage = chatMessage.Replace(match.Groups[0].Value, match.Groups[1].Value);

                match = match.NextMatch();
            }

            match = itemCodeFinder.Match(chatMessage);

            while (match.Success)
            {
                if (match.Groups.Count >= 2)
                    chatMessage = chatMessage.Replace(match.Groups[0].Value, match.Groups[1].Value);

                match = match.NextMatch();
            }

            return chatMessage;
        }
    }
}
