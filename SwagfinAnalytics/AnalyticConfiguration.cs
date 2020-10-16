using System;
using System.Collections.Generic;
using System.Text;

namespace SwagfinAnalytics
{
    public class AnalyticConfiguration
    {
        public AnalyticConfiguration()
        {
            CallBackSessionId = string.Empty;
            AdditionalRequestParameters = new Dictionary<string, string>();
            AdditionalRequestHeaders = new Dictionary<string, string>();
            NextCallBackInSeconds = 3000;
            MaxFailedToAbort = 50;
            //Defaults
            DeviceID = Guid.NewGuid().ToString();
            DeviceName = "DEFAULT";
            EndPointUrl = "http://localhost";
            AppSecretKey = string.Empty;
            AppName = string.Empty;
        }
        public string EndPointUrl { get; set; }
        public string DeviceName { get; set; }
        public string DeviceID { get; set; }
        public string AppName { get; set; }
        public string AppSecretKey { get; set; }
        public int NextCallBackInSeconds { get; internal set; }
        public int MaxFailedToAbort { get; set; }
        public string CallBackSessionId { get; internal set; }
        public bool TrackDeviceHeartBeat { get; set; } = true;
        public Dictionary<string, string> AdditionalRequestParameters { get; set; }
        public Dictionary<string, string> AdditionalRequestHeaders { get; set; }
    }
}
