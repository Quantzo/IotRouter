// Required APIs to use Bluetooth GATT
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

// Required APIs to use built in GUIDs
using Windows.Devices.Enumeration;

// Required APIs for buffer manipulation & async operations
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using System;
using System.Linq;

namespace Bridge
{
    public sealed class BluetoothBridge
    {

        AppServiceConnection _appServiceConnection;
        private GattCharacteristic _notifyCharacteristic;
        private GattCharacteristic _writeCharacteristic;
        private GattCharacteristic _readCharacteristic;

        public BluetoothBridge(AppServiceConnection connection)
        {
            _appServiceConnection = connection;
            Initialize().Wait();
        }



        private async Task Initialize()
        {
            var query = BluetoothLEDevice.GetDeviceSelector();
            var deviceList = await DeviceInformation.FindAllAsync(query);
            var deviceInfo = deviceList.Where(x => x.Name == "UART").FirstOrDefault();

            var bleDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);

            var deviceServices = bleDevice.GattServices;

            var deviceSvc = deviceServices.Where(svc => svc.AttributeHandle == 0x0024).FirstOrDefault();

            var characteristics = deviceSvc.GetAllCharacteristics();

            _notifyCharacteristic = characteristics.Where(x => x.AttributeHandle == 0x0025).FirstOrDefault();
            _writeCharacteristic = characteristics.Where(x => x.AttributeHandle == 0x0028).FirstOrDefault();
            _readCharacteristic = characteristics.Where(x => x.AttributeHandle == 0x002a).FirstOrDefault();

            _notifyCharacteristic.ValueChanged += OnValueChanged;
            await _notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);



        }

        private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {

            var data = DataReader.FromBuffer(args.CharacteristicValue).ReadString(args.CharacteristicValue.Length);
            SendMessageToServer(data);

        }

        public async void OnCommandRecived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;
            if (command.Equals("BluetoothBridge"))
            {
                var value = message["Value"] as string;
                var device = message["Device"] as string;

                var writer = new DataWriter();
                writer.WriteString(value);

                await _writeCharacteristic.WriteValueAsync(writer.DetachBuffer());

            }
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
