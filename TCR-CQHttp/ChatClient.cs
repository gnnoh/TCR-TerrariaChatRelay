using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients;
using TerrariaChatRelay.Helpers;
using Newtonsoft.Json;
using TerrariaChatRelay.Clients.CQHttp;
using Newtonsoft.Json.Linq;
using System.Net;
using WebSocketSharp;
using TCRCQHttp.Helpers;
using System.Text.RegularExpressions;
using TerrariaChatRelay;
using TCRCQHttp.Models;
using TerrariaChatRelay.Command;
using System.Collections.Concurrent;

namespace TCRCQHttp
{
    public class ChatClient : BaseClient
    {
        // CQHttp Variables
        public List<ulong> Group_IDs { get; set; }
        private string BOT_API;
        private string BOT_ACCESS_TOKEN;
        private bool NeedAuth = true;
        private ChatParser chatParser { get; set; }

        // Message Queue
        private CQHttpMessageQueue messageQueue { get; set; }

        // TCR Variables
        private List<IChatClient> parent { get; set; }
        private WebSocket Socket;
        private int errorCounter;

        // Other
        private bool debug = false;

        // WebSocket
        /// <summary>
        /// Allows the maximum request ID value to be configured.
        /// </summary>
        private int MaximumRequestId { get; set; } = int.MaxValue;
        private int MaximumResponseId { get; set; } = int.MaxValue;

        /// <summary>
        /// Used to keep track of the current request ID.
        /// </summary>
        private static int _requestId = 0;
        private static int _responseId = 0;

        /// <summary>
        /// Used to keep track of server responses.
        /// </summary>
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> _responses
            = new ConcurrentDictionary<string, TaskCompletionSource<JObject>>();

        public ChatClient(List<IChatClient> _parent, string bot_api, string bot_access_token, ulong[] group_ids)
            : base(_parent)
        {
            parent = _parent;
            BOT_API = bot_api;
            BOT_ACCESS_TOKEN = bot_access_token;
            chatParser = new ChatParser();
            Group_IDs = group_ids.ToList();

            messageQueue = new CQHttpMessageQueue(500);
            messageQueue.OnReadyToSend += OnMessageReadyToSend;
        }

        /// <summary>
        /// Event fired when a message from in-game is received.
        /// Queues messages to stack messages closely sent to each other.
        /// This will allow TCR to combine messages and reduce messages sent to CQHttp.
        /// </summary>
        public void OnMessageReadyToSend(Dictionary<ulong, Queue<string>> messages)
        {
            foreach (var queue in messages)
            {
                string output = "";

                foreach (var msg in queue.Value)
                {
                    output += msg + '\n';
                }

                if (output.Length > 2000)
                    output = output.Substring(0, 2000);

                SendMessageToCQhttpGroup(output, queue.Key);
            }
        }


        private readonly Regex cqCodeFinder = new Regex(@"\[CQ:\w+?.*?]");
        private readonly Regex cqCodeTypeFinder = new Regex(@"\[CQ:(\w+)");
        private readonly Regex cqCodeParamFinder = new Regex(@",([\w\-.]+?)=([^,\]]+)");
        private readonly Regex cqCodeAtFinder = new Regex(@"\[CQ:at,qq=[0-9al]*\]");
        private readonly Regex cqCodeFaceFinder = new Regex(@"\[CQ:face,id=[0-9]\]");
        public string ConvertCQCodesToNames(Message.GroupMessage chatMessage)
        {
            var match = cqCodeFinder.Match(chatMessage.RawMessage);

            string groupId = Convert.ToString(chatMessage.GroupId);
            string message = chatMessage.RawMessage;

            if (match.Success)
            {
                var cqCode = cqCodeAtFinder.Match(chatMessage.ToString());
                if (cqCode.Success)
                {
                    string qqNumber = cqCode.Value.Substring(10).Substring(0, cqCode.Value.Length - 1);

                    WebSocketMessage request = CreateRequest("{\"action\": \"get_group_member_info\", \"params\": {\"group_id\": " + groupId + ",\"user_id\": " + qqNumber + "}}");
                    JObject response = SendRequest(request);

                    string memberName = response["nickname"].ToString();
                    chatMessage.RawMessage = chatMessage.RawMessage.Replace(cqCode.Value, "[c/00FFFF:@" + memberName + "]");

                    return ConvertCQCodesToNames(chatMessage);
                }

                cqCode = cqCodeFaceFinder.Match(chatMessage.RawMessage);
                if (cqCode.Success)
                {
                    // TODO
                    chatMessage.RawMessage = chatMessage.RawMessage.Replace(cqCode.Value, "-w-");
                    return ConvertCQCodesToNames(chatMessage);
                }

                cqCode = cqCodeTypeFinder.Match(chatMessage.RawMessage);
                chatMessage.RawMessage = "["+chatMessage.RawMessage.Replace(match.Value, cqCode.Value.Substring(4))+"]";
                return ConvertCQCodesToNames(chatMessage);
            }

            return chatMessage.RawMessage;
        }

        /// <summary>
        /// Create a new WebSocket and initiate connection with CQHttp apis. 
        /// Utilizes BOT_ACCESS_TOKEN, BOT_API and GROUP_ID found in Mod Config.
        /// </summary>
        public override void Connect()
        {
            if (BOT_API == "wss://example.com/cqhttp" || Group_IDs.Contains(0))
            {
                PrettyPrint.Log("CQHttp", "Please update your Mod Config. Mod reload required.");

                if (BOT_API == "wss://example.com/cqhttp")
                    PrettyPrint.Log("CQHttp", " Invalid API: BOT_API", ConsoleColor.Yellow);
                if (Group_IDs.Contains(0))
                    PrettyPrint.Log("CQHttp", " Invalid Group Id: 0", ConsoleColor.Yellow);

                PrettyPrint.Log("CQHttp", "Config path: " + new Configuration().FileName);
                Console.ResetColor();
                Dispose();
                return;
            }

            if (BOT_ACCESS_TOKEN == "BOT_ACCESS_TOKEN" || BOT_ACCESS_TOKEN == "")
            {
                PrettyPrint.Log("CQHttp", "Invalid Token: BOT_ACCESS_TOKEN. Assuming that your CQHttp API doesn't need a token to access.", ConsoleColor.Yellow);
                NeedAuth = false;
            }

            if (Main.Config.OwnerUserId == 0)
                PrettyPrint.Log("CQHttp", " Invalid Owner Id: 0", ConsoleColor.Yellow);

            errorCounter = 0;

            Socket = new WebSocket(BOT_API);
            Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            // Socket.Compression = CompressionMethod.Deflate;
            if (NeedAuth)
                Socket.CustomHeaders = new Dictionary<string, string>
                {
                    {"Authorization", "Bearer " + BOT_ACCESS_TOKEN}
                };

            Socket.OnMessage += ProcessMessage;
            Socket.OnError += Socket_OnError;
			if(!debug)
				Socket.Log.Output = (_, __) => { };

            Socket.Connect();

            if (Main.Config.ShowPoweredByMessageOnStartup)
            {
                messageQueue.QueueMessage(Group_IDs,
                    $"**This bot is powered by TerrariaChatRelay**\nUse **{Main.Config.CommandPrefix}help** for more commands!");
				Main.Config.ShowPoweredByMessageOnStartup = true;
				Main.Config.SaveJson();
            }
		}

        /// <summary>
        /// Creates a request for the specified content.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns><see cref="Request"/></returns>
        public WebSocketMessage CreateRequest(string message)
        {
            // Get the next available Request ID.
            int nextRequestId = Interlocked.Increment(ref _requestId);

            if (nextRequestId > MaximumRequestId)
            {
                // Reset the Request ID to 0 and start again.
                Interlocked.Exchange(ref _requestId, 0);

                nextRequestId = Interlocked.Increment(ref _requestId);
            }

            // Create and return the Request object.
            var request = new WebSocketMessage(message, nextRequestId);

            return request;
        }

        /// <summary>
        /// Sends the specified request to the WebSocket server and gets the response.
        /// </summary>
        /// <param name="request">The request to send</param>
        /// <param name="timeout">The timeout (in milliseconds) for the request</param>
        /// <returns>The response result</returns>
        public JObject SendRequest(WebSocketMessage request, int timeout = 10000)
        {
            var tcs = new TaskCompletionSource<JObject>();
            var requestId = request.MessageId;

            try
            {
                string requestString = request.Content.ToString();

                // Add the Request details to the Responses dictionary so that we have   
                // an entry to match up against whenever the response is received.
                _responses.TryAdd(Convert.ToString(requestId), tcs);

                // Send the request to the server.
                if (debug)
                    Console.WriteLine($"Sending request: {requestString}");
                Socket.Send(requestString);
                if (debug)
                    Console.WriteLine("Finished sending request");

                var task = tcs.Task;

                // Wait here until either the response has been received,
                // or we have reached the timeout limit.
                Task.WaitAll(new Task[] { task }, timeout);

                if (task.IsCompleted)
                {
                    // Parse the result, now that the response has been received.
                    JObject response = task.Result;

                    if (debug)
                        Console.WriteLine($"Received response: {response}");

                    // Return the result.
                    return response;
                }
                else // Timeout response.
                {
                    PrettyPrint.Log("CQHttp", $"Client timeout of {timeout} milliseconds has expired, throwing TimeoutException", ConsoleColor.Red);
                    throw new TimeoutException();
                }
            }
            catch (Exception ex)
            {
                PrettyPrint.Log("CQHttp", ex.Message, ConsoleColor.Red);
                throw;
            }
            finally
            {
                // Remove the request/response entry in the 'finally' block to avoid leaking memory.
                _responses.TryRemove(Convert.ToString(requestId), out tcs);
            }
        }

        /// <summary>
        /// Processes messages received over the WebSocket connection.
        /// </summary>
        /// <param name="sender">The sender (WebSocket)</param>
        /// <param name="e">The Message Event Arguments</param>
        private void ProcessMessage(object sender, MessageEventArgs e)
        {
            // Check for Pings.
            if (e.IsPing)
            {
                if (debug)
                    Console.WriteLine("Received Ping");
                return;
            }

            if (debug)
                Console.WriteLine("Processing message");

            // Log when the message is Binary.
            if (e.IsBinary)
            {
                if (debug)
                    Console.WriteLine("Message Type is Binary");
            }

            if (debug)
                Console.WriteLine($"Message Data: {e.Data}");

            JObject message = JObject.Parse(e.Data);

            // If it is sent by CQHttp without request.
            if (message.ContainsKey("post_type"))
            {
                if (message["post_type"].ToString() == "message")
                {
                    if (!CQHttpMessageFactory.TryParseMessage(e.Data, out var msg)) return;

                    try
                    {
                        if (msg.RawMessage != "" && Group_IDs.Contains(msg.GroupId))
                        {
                            string msgout = msg.RawMessage;

                            // Lazy add commands until I take time to design a command service properly
                            //if (ExecuteCommand(chatmsg))
                            //    return;

                            if (!Core.CommandServ.IsCommand(msgout, Main.Config.CommandPrefix))
                            {
                                msgout = ConvertCQCodesToNames(msg);
                                msgout = chatParser.UnEscapeCQHttpRawMessage(msgout);
                            }
                            
                            Permission userPermission;
                            if (msg.Author.SenderId == Main.Config.OwnerUserId)
                                userPermission = Permission.Owner;
                            else if (Main.Config.AdminUserIds.Contains(msg.Author.SenderId))
                                userPermission = Permission.Admin;
                            else if (Main.Config.ManagerUserIds.Contains(msg.Author.SenderId))
                                userPermission = Permission.Manager;
                            else
                                userPermission = Permission.User;

                            var user = new TCRClientUser("CQHttp", msg.Author.SenderNickname, userPermission);
                            TerrariaChatRelay.Core.RaiseClientMessageReceived(this, user, "[c/59de0d:QQ] - ", msgout, Main.Config.CommandPrefix, msg.GroupId);

                            msgout = $"<{msg.Author.SenderNickname}> {msgout}";

                            if (Group_IDs.Count > 1)
                            {
                                // ???
                                messageQueue.QueueMessage(
                                    Group_IDs.Where(x => x != msg.GroupId),
                                    $"**[QQ]** <{msg.Author.SenderNickname}> {msg.RawMessage}");
                            }

                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.Write("[QQ] ");
                            Console.ResetColor();
                            Console.Write(msgout);
                            Console.WriteLine();
                        }
                    }
                    catch (Exception ex)
                    {
                        PrettyPrint.Log("CQHttp", ex.Message, ConsoleColor.Red);
                    }

                }
                    
                return;
            }
                

            // Get the next available Response ID.
            int nextResponseId = Interlocked.Increment(ref _responseId);

            if (nextResponseId > MaximumResponseId)
            {
                // Reset the Response ID to 0 and start again.
                Interlocked.Exchange(ref _responseId, 0);

                nextResponseId = Interlocked.Increment(ref _responseId);
            }

            // Create the Response object.
            var response = new WebSocketMessage(e.Data, nextResponseId);

            // Set the response result.
            if (_responses.TryGetValue(Convert.ToString(response.MessageId), out TaskCompletionSource<JObject> tcs))
            {
                tcs.TrySetResult(message);
            }
            else
            {
                PrettyPrint.Log("CQHttp", "Unexpected response received. ID: " + response.MessageId, ConsoleColor.Red);
            }

            if (debug)
                Console.WriteLine("Finished processing message");
        }

        /// <summary>
        /// Unsubscribes all WebSocket events, then releases all resources used by the WebSocket.
        /// </summary>
        public override void Disconnect()
        {
            // Detach events
            Socket.OnMessage -= ProcessMessage;
            Socket.OnError -= Socket_OnError;

            // Dispose WebSocket client
            if (Socket.ReadyState != WebSocketState.Closed)
                Socket.Close();
			Socket = null;

            // Detach queue from event and dispose
			messageQueue.OnReadyToSend -= OnMessageReadyToSend;
			messageQueue.Clear();
			messageQueue = null;
        }

        /// <summary>
        /// Attempts to reconnect after receiving an error.
        /// </summary>
        private void Socket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            PrettyPrint.Log("CQHttp", e.Message, ConsoleColor.Red);
            Disconnect();

			var restartClient = new ChatClient(parent, BOT_API, BOT_ACCESS_TOKEN, Group_IDs.ToArray());
			PrettyPrint.Log("CQHttp", "Restarting client...", ConsoleColor.Yellow);
			restartClient.Connect();
			parent.Add(restartClient);
			Dispose();
        }

        public override void GameMessageReceivedHandler(object sender, TerrariaChatEventArgs msg)
        {
            if (errorCounter > 2)
                return;
            try
            {
				string outMsg = "";
				string bossName = "";

				if (msg.Player.PlayerId == -1 && msg.Message.EndsWith(" has joined."))
					outMsg = Configuration.PlayerLoggedInFormat;
				else if (msg.Player.PlayerId == -1 && msg.Message.EndsWith(" has left."))
					outMsg = Configuration.PlayerLoggedOutFormat;
				else if (msg.Player.Name != "Server" && msg.Player.PlayerId != -1)
					outMsg = Configuration.PlayerChatFormat;
				else if (msg.Player.Name == "Server" && msg.Message.EndsWith(" has awoken!"))
					outMsg = Configuration.VanillaBossSpawned;
				else if (msg.Player.Name == "Server" && msg.Message == "The server is starting!")
					outMsg = Configuration.ServerStartingFormat;
				else if (msg.Player.Name == "Server" && msg.Message == "The server is stopping!")
					outMsg = Configuration.ServerStoppingFormat;
				else if (msg.Player.Name == "Server")
                    outMsg = Configuration.WorldEventFormat;
				else if (msg.Player.Name == "Server" && msg.Message.Contains("A new version of TCR is available!"))
					outMsg = ":desktop:  **%message%**";
				else
					outMsg = "%message%";

				if (msg.Player != null)
                    outMsg = outMsg.Replace("%playername%", msg.Player.Name)
                                   .Replace("%groupprefix%", msg.Player.GroupPrefix)
                                   .Replace("%groupsuffix%", msg.Player.GroupSuffix);

                outMsg = chatParser.RemoveTerrariaColorAndItemCodes(outMsg);

                if (msg.Message.EndsWith(" has awoken!"))
				{
					bossName = msg.Message.Replace(" has awoken!", "");
					outMsg = outMsg.Replace("%bossname%", bossName);
				}

                // Find the Player Name
				if(msg.Player == null && (msg.Message.EndsWith(" has joined.") || msg.Message.EndsWith(" has left.")))
				{
					string playerName = msg.Message.Replace(" has joined.", "").Replace(" has left.", "");

                    // Suppress empty player name "has left" messages caused by port sniffers
                    if (playerName.IsNullOrEmpty())
                    {
                        // An early return is the easiest way out
                        return;
                    }

					outMsg = outMsg.Replace("%playername%", playerName);
				}

                outMsg = outMsg.Replace("%worldname%", TerrariaChatRelay.Game.World.GetName());
				outMsg = outMsg.Replace("%message%", msg.Message);

				if (outMsg == "" || outMsg == null)
					return;

				messageQueue.QueueMessage(Group_IDs, outMsg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                errorCounter++;

                if(errorCounter > 2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("CQHttp Client has been terminated. Please reload the mod to issue a reconnect.");
                    Console.ResetColor();
                }
            }
        }

        public override void SendMessageToClient(string msg, ulong sourceGroupId)
            => SendMessageToCQhttpGroup(msg, sourceGroupId);

		public override void HandleCommand(ICommandPayload payload, string result, ulong sourceGroupId)
		{
            result = result.Replace("</br>", "\n");
            result = result.Replace("</b>", "**");
            result = result.Replace("</i>", "*");
            result = result.Replace("</code>", "`");
            result = result.Replace("</box>", "```");
            result = result.Replace("</quote>", "> ");

            messageQueue.QueueMessage(sourceGroupId, result);
		}

		public void SendMessageToCQhttpGroup(string message, ulong GroupId)
        {
            message = message.Replace("\\", "\\\\");
            message = message.Replace("\"", "\\\"");
            message = message.Replace("\n", "\\n");
            string json = CQHttpMessageFactory.CreateTextMessage(GroupId, message);

            WebSocketMessage request = CreateRequest(json);
            JObject response = SendRequest(request);

            if (response["status"].ToString() != "ok")
                PrettyPrint.Log("CQHttp", $"Msg: {response["msg"]}, Wording: {response["wording"]}", ConsoleColor.Red);

            if (debug)
            {
                Console.WriteLine(response.ToString());
            }
        }

        public override void GameMessageSentHandler(object sender, TerrariaChatEventArgs msg)
        {

        }
	}
}
