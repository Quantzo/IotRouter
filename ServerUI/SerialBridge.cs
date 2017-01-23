using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace ServerUI
{
    public sealed class SerialBridge
    {
        AppServiceConnection _appServiceConnection;
        SerialDevice _realDevice;
        private MainPage _mainPage;


        public SerialBridge(AppServiceConnection connection, MainPage mainPage, SerialDevice serialDevice)
        {
            _appServiceConnection = connection;
            _mainPage = mainPage;
            _realDevice = serialDevice;
        }

        public void OnCommandRecived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;
            if (command == "RegisterClient" || command == "Disc" || command == "ValueAck")
                WriteToSerialPort(message["Value"] as string);

        }

        public async void ReadSerialPort()
        {
            StringBuilder commandBuilder = new StringBuilder();

            while (true)
            {
                var rbuffer = (new byte[1]).AsBuffer();
                await _realDevice.InputStream.ReadAsync(rbuffer, 1, InputStreamOptions.Partial);

                if ((char)rbuffer.ToArray()[0] != '\n')
                {
                    commandBuilder.Append((char)rbuffer.ToArray()[0]);
                }
                else
                {
                    var value = commandBuilder.ToString();
                    SendMessageToServer(value);
                    //_mainPage.ChangeValue(value);
                    commandBuilder.Clear();
                }

            }
        }


        public async void WriteToSerialPort(string message)
        {
            var writer = new DataWriter(_realDevice.OutputStream);
            writer.WriteString(AddNewLine(message));
            await writer.StoreAsync().AsTask();
            writer.DetachStream();
        }

        private string AddNewLine(string message)
        {
            return message = message + '\r' + '\n';
        }



        private async void SendMessageToServer(string message)
        {
            var command = new ValueSet();
            command.Add("Command", "Bridge");
            command.Add("SendToServer", message);
            var response = await _appServiceConnection.SendMessageAsync(command);
        }
    }
}
