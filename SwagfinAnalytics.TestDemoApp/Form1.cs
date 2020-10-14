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
                //  EndPointUrl = "http://semanticpos.com/analytics/",
                EndPointUrl = "http://127.0.0.1:8443/",

                //Product
                AppName = "MyAnalyticsApp",
                AppSecretKey = "someHashHashKey125456",
                TrackDeviceLastSeen = true,
            };

            this.analytics = new Analytics(analyticConfiguration, new AnalyticsHttpService());
            InitializeComponent();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var statusUpdate = this.analytics.GetAnalyticsStatus();
            if (statusUpdate == AnalyticsStatus.Stopped)
                this.analytics.StartService();
            else
                MessageBox.Show($"Service is {statusUpdate}");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var statusUpdate = this.analytics.GetAnalyticsStatus();
            if (statusUpdate == AnalyticsStatus.Running)
                this.analytics.StopService();
            else
                MessageBox.Show($"Service is {statusUpdate}");
        }
    }
}
