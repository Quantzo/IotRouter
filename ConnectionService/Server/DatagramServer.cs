using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;


namespace ConnectionService.Server
{
    public sealed class DatagramServer : IDisposable
    {
        private DataWriter _writer;
        private DatagramSocket _socket;
        private AppServiceConnection _appServiceConnection;


        public DatagramServer(AppServiceConnection connection)
        {
            _appServiceConnection = connection;
            _appServiceConnection.RequestReceived += OnDataRecived;
        }

        private async void OnDataRecived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
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

                SendDataToDevice(command);
            }

        }

        public async void StartServer(string port)
        {
            _socket = new DatagramSocket();

            var control = _socket.Control;
            _socket.MessageReceived += OnMessageRecived;
            await _socket.BindServiceNameAsync(port);

            var host = new HostName("192.168.137.1");
            var outputStream = await _socket.GetOutputStreamAsync(host, "8001");
            _writer = new DataWriter(outputStream);

        }

        private void OnMessageRecived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            ReadDataFromDevice(args.GetDataReader());

        }



        private void ReadDataFromDevice(DataReader reader)
        {
            reader.InputStreamOptions = InputStreamOptions.Partial;
            reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            var size = reader.UnconsumedBufferLength;
            var message = reader.ReadString(size);
            SendDataToDevice("Ack " + message);
            ProcessMessage(message).Wait();

        }


        private async Task ProcessMessage(string message)
        {
            var request = message.Split(',');
            if (request.Length != 3)
            {
                SendDataToDevice("Unknown command");
                return;
            }
            var command = new ValueSet();
            command.Add("Command", request[0]);
            command.Add("Value", request[2]);
            command.Add("Device", request[1]);

            var responseStatus = await _appServiceConnection.SendMessageAsync(command);


        }


        public async void SendDataToDevice(string message)
        {

            _writer.WriteString(message);
            await _writer.StoreAsync();

        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}
