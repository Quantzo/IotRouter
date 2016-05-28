using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System.Threading;

namespace Bridge
{
    public sealed class StartupTask : IBackgroundTask
    {
        AppServiceConnection _appServiceConnection;
        BackgroundTaskDeferral _serviceDeferral;
        SerialBridge _serialBridge;
        BluetoothBridge _bluetoothBridge;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _serviceDeferral = taskInstance.GetDeferral();
            InitializeService();
        }

        private async void InitializeService()
        {
            _appServiceConnection = new AppServiceConnection();
            _appServiceConnection.PackageFamilyName = "ConnectionService-uwp_5gyrq6psz227t";
            _appServiceConnection.AppServiceName = "App2AppComService";

            // Send a initialize request 
            var res = await _appServiceConnection.OpenAsync();
            if (res == AppServiceConnectionStatus.Success)
            {
                var message = new ValueSet();
                message.Add("Command", "Connect");
                var response = await _appServiceConnection.SendMessageAsync(message);
                if (response.Status == AppServiceResponseStatus.Success)
                {
                    //InitializeSerialBridge();
                    InitializeBluetoothBridge();
                    //_appServiceConnection.RequestReceived += _serialBridge.OnCommandRecived;
                    _appServiceConnection.RequestReceived += _bluetoothBridge.OnCommandRecived;
                }
            }
        }

        private void InitializeSerialBridge()
        {
            _serialBridge = new SerialBridge(_appServiceConnection);
            var asyncAction = ThreadPool.RunAsync((workItem) => _serialBridge.ReadSerialPort());
        }

        private void InitializeBluetoothBridge()
        {
            _bluetoothBridge = new BluetoothBridge(_appServiceConnection);
        }
    }
}
