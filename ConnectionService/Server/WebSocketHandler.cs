using IotWeb.Common.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using static IotWeb.Common.Http.WebSocket;

namespace ConnectionService.Server
{
    class WebSocketHandler : IWebSocketRequestHandler
    {
        private List<WebSocket> Connections = new List<WebSocket>();
        public event DataReceivedHandler MessageRecived;

        public void Connected(WebSocket socket)
        {
            Connections.Add(socket);
            socket.DataReceived += (WebSocket, Frame) => MessageRecived(WebSocket, Frame);
            socket.ConnectionClosed += (WebSocket) => Connections.Remove(WebSocket);
        }

        public bool WillAcceptRequest(string uri, string protocol)
        {
            return true;
        }

        public async Task BroadcastMessage(string message)
        {
            await ThreadPool.RunAsync((workItem) => Connections?.AsParallel().ForAll(WebSocket => WebSocket.Send(message)));
        }
    }
}
