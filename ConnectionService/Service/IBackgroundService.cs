using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;

namespace ConnectionService.Service
{
    public interface IBackgroundService
    {
        string ServiceName {get; }
        void OnDataReceived(string data, AppServiceRequestReceivedEventArgs args);
    }
}
