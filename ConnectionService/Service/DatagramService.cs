using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;


namespace ConnectionService.Service
{
    public sealed class DatagramService : IDisposable, IBackgroundService
    {
        private DataWriter _writer;
        private DatagramSocket _socket;
        public string ServiceName => _name;
        private readonly string _name;
        private readonly AppServiceConnection _conection;

        public DatagramService(string name, AppServiceConnection conection)
        {
            _name = name;
            _conection = conection;

        }
        public async void OnDataReceived(string data, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();
            var returnMessage = new ValueSet();
            returnMessage.Add("Status", "Success");
            var responseStatus = await args.Request.SendResponseAsync(returnMessage);
            messageDeferral.Complete();

            SendDataToSocket(data);


        }

        public async void StartServer(string port)
        {
            _socket = new DatagramSocket();

            var control = _socket.Control;
            _socket.MessageReceived += OnMessageRecived;
            await _socket.BindServiceNameAsync(port);
        }

        private void OnMessageRecived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            _writer = new DataWriter(sender.OutputStream);
            ReadMessageFromSocket(args.GetDataReader());
        }



        private void ReadMessageFromSocket(DataReader reader)
        {
            reader.InputStreamOptions = InputStreamOptions.Partial;
            reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            var size = reader.UnconsumedBufferLength;
            var message = reader.ReadString(size);
            ProcessMessage(message).Wait();

        }


        private async Task ProcessMessage(string message)
        {
            var command = new ValueSet();
            command.Add("Command", "Message");
            command.Add("Route", ServiceName);
            command.Add("Message", message);
            var responseStatus = await _conection.SendMessageAsync(command);
        }


        public async void SendDataToSocket(string message)
        {
            if(_writer != null)
            {
                _writer.WriteString(message);
                await _writer.StoreAsync();
            }

        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}
