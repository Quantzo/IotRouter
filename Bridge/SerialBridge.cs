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

namespace Bridge
{
    public sealed class SerialBridge
    {
        AppServiceConnection _appServiceConnection;
        SerialDevice _realDevice;

        public SerialBridge(AppServiceConnection connection)
        {
            _appServiceConnection = connection;
            Initialize().Wait();
        }

        public void OnCommandRecived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;
            if (command.Equals("SerialBridge"))
            {
                var value = message["Value"] as string;
                var device = message["Device"] as string;
                WriteToSerialPort(device, value);

            }
        }

        private async Task Initialize()
        {
            var device = SerialDevice.GetDeviceSelectorFromUsbVidPid(0x1F00, 0x2012);
            var usbDevices = await DeviceInformation.FindAllAsync(device);
            var currentDevice = usbDevices.FirstOrDefault();
            _realDevice = await SerialDevice.FromIdAsync(currentDevice.Id);
            _realDevice.Handshake = SerialHandshake.XOnXOff;
            _realDevice.BaudRate = 9600;
            _realDevice.Parity = SerialParity.None;
            _realDevice.StopBits = SerialStopBitCount.One;
            _realDevice.DataBits = 8;
            _realDevice.IsDataTerminalReadyEnabled = true;
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
                    SendMessageToServer(commandBuilder.ToString());
                    commandBuilder.Clear();
                }

            }
        }


        public async void WriteToSerialPort(string deviceNumber, string value)
        {
            var writer = new DataWriter(_realDevice.OutputStream);
            writer.WriteString(AddNewLine(deviceNumber + ',' + value));
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
