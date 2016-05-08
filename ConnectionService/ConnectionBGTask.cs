using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.AppService;
using Windows.System.Threading;
using Windows.Networking.Sockets;
using System.IO;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using ConnectionService.Server;

namespace ConnectionService
{
    public sealed class ConnectionBGTask : IBackgroundTask
    {
        BackgroundTaskDeferral _serviceDeferral;
        AppServiceConnection _appServiceConnection;
        //DatagramServer _server;
        AzureIoTHubConnection _azureConnection;


        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _serviceDeferral = taskInstance.GetDeferral();
            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;

            if (appService != null && appService.Name == "App2AppComService")
            {
                _appServiceConnection = appService.AppServiceConnection;
                _appServiceConnection.RequestReceived += OnRequestReceived;

                //_server = new DatagramServer(_appServiceConnection);
                //var asyncAction = ThreadPool.RunAsync((workItem) => _server.StartServer("8000"));
                _azureConnection = new AzureIoTHubConnection(_appServiceConnection);
                var asyncAction = ThreadPool.RunAsync((workItem) => _azureConnection.ReceiveCommands());

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
            }
        }

    }
}
