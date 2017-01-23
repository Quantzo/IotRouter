using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.AppService;
using Windows.System.Threading;
using ConnectionService.Service;
using ConnectionService.Service.WebSocketService;

namespace ConnectionService
{
    public sealed class ConnectionBGTask : IBackgroundTask
    {
        BackgroundTaskDeferral _serviceDeferral;
        AppServiceConnection _appServiceConnection;
        private List<IBackgroundService> _services = new List<IBackgroundService>();


        private void ConfigureServices()
        {
            void AddService(IBackgroundService service, WorkItemHandler action)
            {
                _services.Add(service);
                var Aaction = ThreadPool.RunAsync(action);
            }

            var DatagramServer = new DatagramService("datagram", _appServiceConnection);
            AddService(DatagramServer, (workItem) => DatagramServer.StartServer("8001"));

            var WebSocketServer = new WebSocketServerService("websock", _appServiceConnection, 5);
            AddService(WebSocketServer, (workItem) => WebSocketServer.StartServer(8080, "/sockets/"));

            //var AzureConnection = new AzureIoTHubService("azure", _appServiceConnection);
            //AddService(AzureConnection, (workItem) => AzureConnection.ReceiveCommands());

        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _serviceDeferral = taskInstance.GetDeferral();
            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;

            if (appService != null && appService.Name == "App2AppComService")
            {
                _appServiceConnection = appService.AppServiceConnection;
                _appServiceConnection.RequestReceived += OnRequestReceived;
                ConfigureServices();
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {            
            var message = args.Request.Message;
            string command = message["Command"] as string;

            switch (command)
            {
                case "Connect":
                    {
                        var messageDeferral = args.GetDeferral();
                        var returnMessage = new ValueSet();
                        returnMessage.Add("Status", "Success");
                        var responseStatus = await args.Request.SendResponseAsync(returnMessage);
                        messageDeferral.Complete();
                        break;
                    }

                case "Quit":
                    {
                        _serviceDeferral.Complete();
                        break;
                    }

                case "Message":
                    {
                        var route = message["Route"] as string;
                        var data = message["Message"] as string;
                        _services.FindAll(i => i.ServiceName == route).ForEach(i => i.OnDataReceived(data, args));
                        break;
                    }
            }
        }

    }
}
