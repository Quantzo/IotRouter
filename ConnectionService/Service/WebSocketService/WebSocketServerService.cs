using IotWeb.Common.Http;
using IotWeb.Server;
using static Newtonsoft.Json.JsonConvert;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Newtonsoft.Json;

namespace ConnectionService.Service.WebSocketService
{
    public sealed class WebSocketServerService: IBackgroundService
    {
        private AppServiceConnection _appServiceConnection;
        private BaseHttpServer _httpServer;
        private WebSocketHandler _webSockets;
        private string _currentValue = "WebSocketValue";
        private IdHelper _portMapping;
        private readonly string _name;

        string IBackgroundService.ServiceName => _name;

        public WebSocketServerService(string name, AppServiceConnection conection, int maxNumber)
        {
            _name = name;
            _appServiceConnection = conection;       
            _portMapping = new IdHelper(maxNumber);
        }

        public async void OnDataReceived(string data, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();
            var returnMessage = new ValueSet();
            returnMessage.Add("Status", "Success");
            var responseStatus = await args.Request.SendResponseAsync(returnMessage);
            messageDeferral.Complete();

            await _webSockets.BroadcastMessage(data);
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
            var command = new ValueSet { { "Command", "Message" }, { "Message", message } };
            var responseStatus = await _appServiceConnection.SendMessageAsync(command);
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
                                break;
                            }
                        case "Disc":
                            {
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
