using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SwagfinAnalytics
{
    public interface IAnalyticsHttpService
    {
        Task<string> RequestAsync(string url, HttpMethod httpMethod, Dictionary<string, string> parameters = null, Dictionary<string, string> headers = null);
    }
}
