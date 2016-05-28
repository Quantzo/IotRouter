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
        public WebSocketServer(AppServiceConnection connection)
        {
            _appServiceConnection = connection;
            _appServiceConnection.RequestReceived += OnRequestReceived;
            
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;
            if (command.Equals("Bridge"))
            {
                command = message["SendToServer"] as string;

                var messageDeferral = args.GetDeferral();
                var returnMessage = new ValueSet();
                returnMessage.Add("Status", "Success");
                var responseStatus = await args.Request.SendResponseAsync(returnMessage);
                messageDeferral.Complete();

                await _webSockets.BroadcastMessage(command);
            }
        }

        public async Task StartServer(int port, string uri)
        {
            _httpServer = new HttpServer(port);
            _webSockets = new WebSocketHandler();
            _webSockets.MessageRecived += _webSockets_MessageRecived;
            _httpServer.AddWebSocketRequestHandler(uri, _webSockets);
            await ThreadPool.RunAsync((workItem) => _httpServer.Start());
        }

        private async void _webSockets_MessageRecived(WebSocket socket, string frame)
        {
            try
            {
                var recivedData = DeserializeObject<ClientMessage>(frame);
                await ProcessMessage(recivedData);
            }
            catch (JsonReaderException e)
            {

            }
        }

        private async Task ProcessMessage(ClientMessage message)
        {
            if (message.Command != null && message.Value != null)
            {
                var command = new ValueSet();
                command.Add("Command", message.Command);
                command.Add("Value", message.Value);

                var responseStatus = await _appServiceConnection.SendMessageAsync(command);
            }
        }
    }
    internal class ClientMessage
    {
        public string Command { get; set; }
        public string Value { get; set; }
    }
}
