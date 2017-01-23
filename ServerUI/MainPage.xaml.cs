using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ServerUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private AppServiceConnection _appServiceConnection;
        //SerialBridge _serialBridge;

        //public string CurrentValue
        //{
        //    get { return _currentValue; }
        //    set
        //    {
        //        _currentValue = value;
        //        Bindings.Update();
        //    }
        //}

        //private string _currentValue;
        //public string CurrentCount { get { return _currentCount; }
        //    set
        //    {
        //        _currentCount = value;
        //        Bindings.Update();
        //    }
        //}

        //private string _currentCount;
        public bool KeyEnabled { get { return _keyEnabled; }
            set
            {
                _keyEnabled = value;
                Bindings.Update();
            }
        }
        private bool _keyEnabled;



        public bool Connection
        {
            get { return _connecton; }
            set
            {
                _connecton = value;
                Bindings.Update();
            }
        }
        private bool _connecton = false;

        //private int _count;

        public string ConsoleText
        {
            get { return _text; }
            set
            {
                _text = value;
                Bindings.Update();
            }
        }
        private string _text;


        private string _data;

        public string DataText
        {
            get { return _data;}
            set { _data = value;}
        }


        public MainPage()
        {

            this.InitializeComponent();

            KeyEnabled = true;
            //CurrentValue = "X";
            //CurrentCount = "X";
        }

        public async void OnClick()
        {

            
            //_count = 0;
            //await ChangeCount(0);
            
            await Initialize();
            
        }

        public async void OnDatagramSend()
        {
            var message = new ValueSet
            {
                { "Command", "Message" },
                {"Route","datagram" },
                {"Message", DataText }
            };
            var response = await _appServiceConnection.SendMessageAsync(message);
        }
        public async void OnWebSocketSend()
        {
            var message = new ValueSet
            {
                { "Command", "Message" },
                {"Route","websock" },
                {"Message", DataText }
            };
            var response = await _appServiceConnection.SendMessageAsync(message);
        }

        private async Task Initialize()
        {
            _appServiceConnection = new AppServiceConnection
            {
                PackageFamilyName = "ConnectionService-uwp_90mfhvp51y788",
                AppServiceName = "App2AppComService"
            };

            // Send a initialize request 
            var res = await _appServiceConnection.OpenAsync();
            if (res == AppServiceConnectionStatus.Success)
            {
                KeyEnabled = false;
                Connection = true;
                var message = new ValueSet { { "Command", "Connect" } };

                var response = await _appServiceConnection.SendMessageAsync(message);
                if (response.Status == AppServiceResponseStatus.Success)
                {
                //    await InitializeSerialBridge();
                    
                //    _appServiceConnection.RequestReceived += _serialBridge.OnCommandRecived;
                    _appServiceConnection.RequestReceived += AppServiceConnectionOnRequestReceived;

                }
            }
        }

        private void AppServiceConnectionOnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;
            if(command == "Message")
            {
                var val = message["Message"] as string;
                Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => ConsoleText += $"{val}{Environment.NewLine}" );
            }

            //if (command.Equals("RegisterClient"))
            //{
            //    ChangeCount(1);
            //}
            //else if(command.Equals("Disc"))
            //{
            //    ChangeCount(-1);
            //}
        }

        //private async Task ChangeCount(int num)
        //{
        //    _count += num;
        //    await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => CurrentCount = _count.ToString());
        //}

        //public  async Task ChangeValue(string val)
        //{
        //    await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => CurrentValue = val);
        //}
        //private async Task InitializeSerialBridge()
        //{
        //    var device = SerialDevice.GetDeviceSelectorFromUsbVidPid(0x03EB, 0x2122);
        //    var usbDevices = await DeviceInformation.FindAllAsync(device);
        //    var currentDevice = usbDevices.FirstOrDefault();
        //    var serialDevice = await SerialDevice.FromIdAsync(currentDevice.Id);
        //    serialDevice.Handshake = SerialHandshake.XOnXOff;
        //    serialDevice.BaudRate = 9600;
        //    serialDevice.Parity = SerialParity.None;
        //    serialDevice.StopBits = SerialStopBitCount.One;
        //    serialDevice.DataBits = 8;
        //    serialDevice.IsDataTerminalReadyEnabled = true;

        //    _serialBridge = new SerialBridge(_appServiceConnection, this, serialDevice);
        //    var asyncAction = ThreadPool.RunAsync((workItem) => _serialBridge.ReadSerialPort());
        //}


    }
}
