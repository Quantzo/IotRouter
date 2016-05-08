using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace ConnectionService.Server
{
    public sealed class AzureIoTHubConnection
    {

        private const string DeviceConnectionString = "HostName=RouterIoTHub.azure-devices.net;DeviceId=RouterIoT;SharedAccessKey=2EAmBtAxqztUZ7Z/LXr/sKcW0BoRyNrusxD8x9XBDDY=";
        private AppServiceConnection _appServiceConnection;
        private DeviceClient deviceClient;


        public AzureIoTHubConnection(AppServiceConnection connection)
        {
            _appServiceConnection = connection;
            deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString);
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
                await SendEvent(command);
            }

        }


        async Task SendEvent(string message)
        {
            var azureEntry = new
            {
                DeviceId = "RouterIoT",
                Timestamp = DateTime.Now.Ticks,
                Value = message
            };
            var eventMessage = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(azureEntry)));
            await deviceClient.SendEventAsync(eventMessage);
        }


        public async void ReceiveCommands()
        {
            while (true)
            {
                var receivedMessage = await deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    try
                    {
                        var azureMessage = JsonConvert.DeserializeObject<AzureMessage>(messageData);
                        await ProcessMessage(azureMessage);
                    }
                    catch(JsonReaderException e)
                    {

                    }
                    finally
                    {
                        await deviceClient.CompleteAsync(receivedMessage);
                    }
                    
                    
                }
            }
        }


        private async Task ProcessMessage(AzureMessage message)
        {
            if(message.Command != null && message.Device != null && message.Value != null)
            {
                var command = new ValueSet();
                command.Add("Command", message.Command);
                command.Add("Device", message.Device);
                command.Add("Value", message.Value);

                var responseStatus = await _appServiceConnection.SendMessageAsync(command);
            }



        }

    }

    internal class AzureMessage
    {
        public string Command { get; set; }
        public string Device { get; set; }
        public string Value { get; set; }
    }
}


