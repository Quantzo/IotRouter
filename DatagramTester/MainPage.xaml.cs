using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DatagramTester
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private DatagramSocket _socket;
        private DataWriter _writer;

        private string _hostName;
        public string HostName { get {return _hostName;} set { _hostName = value; Bindings.Update(); } }
        private string _consoleText;
        public string ConsoleText { get { return _consoleText; } set { _consoleText = value; Bindings.Update(); } }
        private bool _sendEnabled = true;
        public bool SendEnabled { get { return _sendEnabled; } set { _sendEnabled = value; Bindings.Update(); }}
        public bool Connected = false;
        private string _buttonConnect = "Connect";
        public string ButtonContent { get { return _buttonConnect; } set { _buttonConnect = value; Bindings.Update(); } }
        public MainPage()
        {
            this.InitializeComponent();
        }


        public async void OnClick()
        {
            if (!Connected)
            {
                _socket = new DatagramSocket();
                _socket.MessageReceived += SocketOnMessageReceived;
                _writer = new DataWriter(_socket.OutputStream);

                var host = new HostName(HostName);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => SendEnabled = false);
                await _socket.ConnectAsync(host, "8001");
                await SendMessage("Hi Connected");
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => SendEnabled = true);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => ButtonContent = "Send");
                Connected = true;
            }
            else
            {
                await SendMessage(HostName);
            }


        }

        private void SocketOnMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var reader = args.GetDataReader();
            reader.InputStreamOptions = InputStreamOptions.Partial;
            reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            var size = reader.UnconsumedBufferLength;
            var message = reader.ReadString(size);
            Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => ConsoleText += $"{message}{Environment.NewLine}");
        }

        private async Task SendMessage(string mess)
        {
            _writer.WriteString(mess);
            await _writer.StoreAsync();
        }
    }
}
