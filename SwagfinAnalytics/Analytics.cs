using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace SwagfinAnalytics
{
    public class Analytics : IAnalytics
    {
        private readonly AnalyticConfiguration configurations;
        private readonly IAnalyticsHttpService analyticsClient;
        private ConcurrentQueue<Trait> pendingToSendTraits { get; set; } = new ConcurrentQueue<Trait>();
        private AnalyticsStatus analyticsStatus { get; set; }
        private Timer analyticsTimer { get; set; }

        public Analytics(AnalyticConfiguration analyticsConfigurations, IAnalyticsHttpService analyticsHttpService)
        {
            this.configurations = analyticsConfigurations;
            this.analyticsClient = analyticsHttpService;
            this.pendingToSendTraits = new ConcurrentQueue<Trait>();
            this.analyticsStatus = AnalyticsStatus.Stopped;
        }

        public void StartService()
        {
            AnalyticsLogger.LogInformation("Starting Analytics Service");
            this.analyticsStatus = AnalyticsStatus.Starting;
            //Create an Instance of Timer
            analyticsTimer = new Timer();
            analyticsTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            analyticsTimer.Interval = this.configurations.NextCallBackInSeconds;
            analyticsTimer.Enabled = true;
        }
        public void StopService()
        {
            AnalyticsLogger.LogInformation("Stopping Analytics Service");
            this.analyticsStatus = AnalyticsStatus.Stopping;
            analyticsTimer.Enabled = false;
            analyticsTimer = null;
            this.analyticsStatus = AnalyticsStatus.Stopped;
            //Unprocessed
        }

        public AnalyticConfiguration GetConfigurations() => configurations;
        public AnalyticsStatus GetAnalyticsStatus() => this.analyticsStatus;

        public void Track(string traitKey, string traitValue) => Track(new Trait { Key = traitKey, Value = traitValue });
        public void Track(Trait trait) => this.pendingToSendTraits.Enqueue(trait);

        public void Track(IList<Trait> traits)
        {
            foreach (Trait trait in traits)
                this.Track(trait);
        }

        private async void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            analyticsTimer.Stop();
            this.analyticsStatus = AnalyticsStatus.Running;
            //Track Last Seen
            if (configurations.TrackDeviceHeartBeat)
                this.Track(new Trait { Key = "heartBeat", Value = DateTime.Now.ToString() });
            //Proceed
            var readyToSendTraits = this.pendingToSendTraits.Where(x => x.SentSuccesfully != true && x.NextSending < DateTime.Now).Take(100);
            //Check if Null
            if (readyToSendTraits != null && readyToSendTraits.Count() > 0)
            {

                AnalyticsLogger.LogInformation($"Executing ({readyToSendTraits.Count()}) Analytics");
                //Append All Traits
                string allTraitsAppend = string.Empty;
                foreach (Trait trait in readyToSendTraits)
                    allTraitsAppend = $"{allTraitsAppend}{trait.Key} : {trait.Value}{Environment.NewLine}";

                try
                {
                    AnalyticsLogger.LogInformation($"Sending All Traits");
                    this.analyticsStatus = AnalyticsStatus.Seeding;
                    //Parameters
                    Dictionary<string, string> parameters = new Dictionary<string, string>();
                    parameters.Add("traitKey", "rawData");
                    parameters.Add("traitValue", allTraitsAppend);
                    //Add Configs
                    parameters.Add("appName", configurations.AppName);
                    parameters.Add("deviceName", configurations.DeviceName);
                    parameters.Add("deviceId", configurations.DeviceID);
                    parameters.Add("sessionId", configurations.CallBackSessionId);
                    //Add Default
                    foreach (var param in configurations.AdditionalRequestParameters)
                        parameters.Add(param.Key, param.Value);

                    //Headers
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    headers.Add("appSecret", configurations.AppSecretKey);

                    //Add Default
                    foreach (var header in configurations.AdditionalRequestHeaders)
                        headers.Add(header.Key, header.Value);
                    //Call Http Client
                    string response = await this.analyticsClient.RequestAsync(configurations.EndPointUrl, System.Net.Http.HttpMethod.Post, parameters, headers);
                    this.analyticsStatus = AnalyticsStatus.Running;
                    //Update
                    foreach (Trait trait in readyToSendTraits)
                    {
                        trait.SentSuccesfully = true;
                        trait.FailedCount = 0;
                        AnalyticsLogger.LogInformation($"Trait: {trait.Id} Sent Successfully, Response: {response}");
                        RemoveTraitFromQueue(trait);
                    }

                    //Set Next Call Back
                    SetNextCallBackFromServerResponse(response);
                    //Detected One has been Sent try to Reque
                    RequeueAllUnSent();

                }
                catch (Exception ex)
                {
                    foreach (Trait trait in readyToSendTraits)
                    {
                        AnalyticsLogger.LogError($"Error Sending Trait: {trait.Id}, Exception: {ex.Message}");
                        //Mark to be Requeued
                        trait.FailedCount++;
                        trait.NextSending = trait.NextSending.AddMinutes(trait.FailedCount);
                        AnalyticsLogger.LogInformation($"Added Trait: {trait.Id} to Queue To Send Later at: {trait.NextSending}");
                    }

                    //Maximum Fails to Stop
                    if (GetFailedTraitsCount() >= configurations.MaxFailedToAbort)
                        StopService();
                }
                //RENABLE THE CLOCK
                analyticsTimer.Start();
            }
        }

        private void SetNextCallBackFromServerResponse(string response)
        {
            try
            {
                AnalyticsLogger.LogInformation($"Attempting to Get Session ID and Next CallBack from Response");

                var dataX = response.Split('|');
                int nextCallback = 0;
                //Check
                if (dataX.Count() < 2)
                    throw new Exception("Unable to Decode or Retrieve Expected Server Response");

                int.TryParse(dataX[0], out nextCallback);
                //Check
                if (nextCallback < 1000)
                    nextCallback = 1000;
                //Check if CallBack Changed
                if (nextCallback != this.configurations.NextCallBackInSeconds)
                {
                    this.configurations.NextCallBackInSeconds = nextCallback;
                    analyticsTimer.Interval = this.configurations.NextCallBackInSeconds;
                    AnalyticsLogger.LogInformation($"UPDATED: NextCallBack: {nextCallback}");
                }

                //Get Session ID
                string nextSessionId = string.Empty;
                nextSessionId = dataX[1];
                //Check if Empty
                if (!string.IsNullOrWhiteSpace(nextSessionId) && nextSessionId != configurations.CallBackSessionId)
                {
                    this.configurations.CallBackSessionId = nextSessionId;
                    AnalyticsLogger.LogInformation($"UPDATED: NextSessionID: {nextSessionId}");
                }

            }
            catch (Exception ex)
            {
                AnalyticsLogger.LogError($"ERROR: FAILED Get Session ID and Next CallBack from Response: {ex.Message}");
            }
        }

        private void RemoveTraitFromQueue(Trait trait)
        {
            AnalyticsLogger.LogInformation($"Attempting to Remove Trait: {trait.Id}");
            //Remove Trait
            Trait traitRemoval = trait;
            bool removed = this.pendingToSendTraits.TryDequeue(out traitRemoval);
            if (removed)
                AnalyticsLogger.LogInformation($"Trait: {trait.Id} REMOVED from Queue");
            else
                AnalyticsLogger.LogWarning($"Trait: {trait.Id} WAS NOT REMOVED from Queue");
            //Check if Null
        }

        private void RequeueAllUnSent()
        {
            var allUnsent = this.pendingToSendTraits.Where(x => x.SentSuccesfully != true && x.FailedCount > 0);
            if (allUnsent != null && allUnsent.Count() > 0)
            {
                AnalyticsLogger.LogInformation($"Requeing ({allUnsent.Count()}) Analytics");
                foreach (Trait trait in allUnsent)
                {
                    trait.NextSending = DateTime.Now.AddSeconds(trait.FailedCount); //Based on Failure Counts
                    trait.FailedCount = 0;
                }
            }
        }

        private int GetFailedTraitsCount() => this.pendingToSendTraits.Where(x => x.SentSuccesfully != true && x.FailedCount > 0).Count();


    }
    public enum AnalyticsStatus
    {
        Running,
        Starting,
        Stopped,
        Stopping,
        Seeding
    }


}
