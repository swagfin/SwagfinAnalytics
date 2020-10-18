using SwagfinAnalytics.TestDemoApp.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SwagfinAnalytics.TestDemoApp
{
    public partial class Form1 : Form
    {
        IAnalytics analytics { get; }
        public Form1()
        {
            AnalyticConfiguration analyticConfiguration = new AnalyticConfiguration
            {
                DeviceID = "3556-5785-5255-TEST-0001", //3CA3-52FB-86E7-80B3-2F3C
                DeviceName = Environment.MachineName,
                EndPointUrl = "http://127.0.0.1:8443/",
                CallBackSessionId = Settings.Default.LastAnalyticsSessionID,
                //Product
                AppName = "MyAnalyticsApp",
                AppSecretKey = "someHashHashKey125456",
                TrackDeviceHeartBeat = true,
            };



            this.analytics = new AnalyticsManager(analyticConfiguration, new AnalyticsHttpService(), new AnalyticsLogger());
            this.analytics.Track("System Startups", DateTime.Now.ToString());
            this.analytics.Track("System CrushError", "Unable to Connect to Database");
            this.analytics.Track("System Error", "Authentication Failed");

            InitializeComponent();

            sessionIDBox.Text = analyticConfiguration.CallBackSessionId;


        }

        private void button1_Click(object sender, EventArgs e)
        {

            var statusUpdate = this.analytics.GetAnalyticsStatus();
            if (statusUpdate == AnalyticsStatus.Stopped)
                this.analytics.StartService();
            else
                MessageBox.Show($"Service is {statusUpdate}");

            this.analytics.Track("Analytics Service Start", DateTime.Now.ToString());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var statusUpdate = this.analytics.GetAnalyticsStatus();
            if (statusUpdate == AnalyticsStatus.Running)
                this.analytics.StopService();
            else
                MessageBox.Show($"Service is {statusUpdate}");

            this.analytics.Track("Analytics Service Stopped", DateTime.Now.ToString());

        }

        private void button1_MouseHover(object sender, EventArgs e)
        {
            this.analytics.Track("User Hovering", "On Start Analytic Button");
        }

        private void button2_MouseHover(object sender, EventArgs e)
        {
            this.analytics.Track("User Hovering", "On Stop Analytic Service Button");
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            this.analytics.Track("User Clicking", "Main Wiindow");
        }
    }


    public class AnalyticsManager : Analytics
    {
        public AnalyticsManager(AnalyticConfiguration analyticsConfigurations, IAnalyticsHttpService analyticsHttpService, AnalyticsLogger analyticsLogger) : base(analyticsConfigurations, analyticsHttpService, analyticsLogger)
        {
        }

        protected override void OnSessionIdRenewed(string newSessionId)
        {
            Settings.Default.LastAnalyticsSessionID = newSessionId;
            Settings.Default.Save();
            base.OnSessionIdRenewed(newSessionId);
        }
    }
}
