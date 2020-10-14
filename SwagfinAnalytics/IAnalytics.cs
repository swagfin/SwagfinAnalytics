using System;
using System.Collections.Generic;

namespace SwagfinAnalytics
{
    public interface IAnalytics
    {
        AnalyticConfiguration GetConfigurations();
        AnalyticsStatus GetAnalyticsStatus();
        void Track(Trait trait);
        void Track(IList<Trait> traits);
        void StartService();
        void StopService();
    }
}
