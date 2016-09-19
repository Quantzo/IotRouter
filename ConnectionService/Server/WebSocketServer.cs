using IotWeb.Common.Http;
using IotWeb.Server;
using static Newtonsoft.Json.JsonConvert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Newtonsoft.Json;

namespace ConnectionService.Server
{
    public sealed class WebSocketServer
    {
        private AppServiceConnection _appServiceConnection;
        private BaseHttpServer _httpServer;
        private WebSocketHandler _webSockets;
        private string _currentValue;
        private IdHelper _portMapping;

        public WebSocketServer(AppServiceConnection connection)
        {
            _appServiceConnection = connection;
            _appServiceConnection.RequestReceived += OnRequestReceived;
            
            _portMapping = new IdHelper(5);
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;
            if (command.Equals("Bridge"))
            {
                _currentValue = message["SendToServer"] as string;

                var messageDeferral = args.GetDeferral();
                var returnMessage = new ValueSet();
                returnMessage.Add("Status", "Success");
                var responseStatus = await args.Request.SendResponseAsync(returnMessage);
                messageDeferral.Complete();

                //await _webSockets.BroadcastMessage(command);
            }
        }

        public async void StartServer(int port, string uri)
        {
            _httpServer = new HttpServer(port);
            _webSockets = new WebSocketHandler(_portMapping);
            _webSockets.MessageRecived += _webSockets_MessageRecived;
            _httpServer.AddWebSocketRequestHandler(uri, _webSockets);
            await ThreadPool.RunAsync((workItem) => _httpServer.Start());
        }

        private async void _webSockets_MessageRecived(string guid, WebSocket socket, string message)
        {
            try
            {
                var recivedData = DeserializeObject<ServerMessage>(message);
                await ProcessMessage(guid, recivedData);
                
            }
            catch (JsonReaderException e)
            {
                var messageErr = new ServerMessage()
                {
                    ClientID = guid,
                    Command = "Error",
                    Value = "Wrong message's format"
                };
                _webSockets.SendMessage(guid, messageErr);

            }
        }

        private async Task ProcessMessage(string guid, ServerMessage message)
        {

            if (_portMapping.IsBinded(guid))
            {
                if (message.Command != null && message.Value != null)
                {
                    switch (message.Command)
                    {
                        case "RegisterClient":
                            {
                                var command = new ValueSet { { "Command", message.Command }, { "Value", message.Value } };

                                var responseStatus = await _appServiceConnection.SendMessageAsync(command);
                                break;
                            }

                        case "ValueReq":
                            {
                                var response = new ServerMessage()
                                {
                                    ClientID = guid,
                                    Command = "Value",
                                    Value = _currentValue

                                };
                                await _webSockets.SendMessage(guid, response);
                                break;
                            }
                        case "ValueAck":
                            {
                                var command = new ValueSet { { "Command", message.Command }, { "Value", message.Value } };

                                var responseStatus = await _appServiceConnection.SendMessageAsync(command);
                                break;
                            }
                        case "Disc":
                            {
                                var command = new ValueSet { { "Command", message.Command }, { "Value", message.Value } };

                                var responseStatus = await _appServiceConnection.SendMessageAsync(command);
                                _portMapping.UnBind(guid);
                                break;
                            }
                        default:
                        {
                                var messageErr = new ServerMessage()
                                {
                                    ClientID = guid,
                                    Command = "Error",
                                    Value = "Unknown command"
                                };
                                _webSockets.SendMessage(guid, messageErr);
                                break;
                        }
                    }
                    
                }
                else
                {
                    var messageErr = new ServerMessage()
                    {
                        ClientID = guid,
                        Command = "Error",
                        Value = "Wrong message's format"
                    };
                    _webSockets.SendMessage(guid, messageErr);

                }
            }
            else
            {
                var messageErr = new ServerMessage()
                {
                    ClientID = guid,
                    Command = "Error",
                    Value = "Please close connection"
                };
                _webSockets.SendMessage(guid, messageErr);
            }
            
        }
    }
    internal class ServerMessage
    {
        public string ClientID { get; set; }
        public string Command { get; set; }
        public string Value { get; set; }
    }


}
