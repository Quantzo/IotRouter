using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace ConnectionService.Service
{
    public sealed class AzureIoTHubService: IBackgroundService
    {

        private const string DeviceConnectionString = "HostName=RouterIoTHub.azure-devices.net;DeviceId=RouterIoT;SharedAccessKey=2EAmBtAxqztUZ7Z/LXr/sKcW0BoRyNrusxD8x9XBDDY=";
        private readonly AppServiceConnection _conection;
        private DeviceClient deviceClient;
        private readonly string _name;

        public string ServiceName => _name;

        public AzureIoTHubService(string name, AppServiceConnection conection)
        {
            _conection = conection;
            _name = name;
            deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString);
        }


        public async void OnDataReceived(string data, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();
            var returnMessage = new ValueSet();
            returnMessage.Add("Status", "Success");
            var responseStatus = await args.Request.SendResponseAsync(returnMessage);
            messageDeferral.Complete();

            await SendEvent(data);


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
                command.Add("Command", "Message");
                command.Add("Message", JsonConvert.SerializeObject(message));
                var responseStatus = await _conection.SendMessageAsync(command);
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


