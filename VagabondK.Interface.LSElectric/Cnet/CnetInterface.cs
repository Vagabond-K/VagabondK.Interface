using System;
using System.Collections.Generic;
using System.Text;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.LSElectric.Cnet;

namespace VagabondK.Interface.LSElectric.Cnet
{
    public class CnetInterface : PollingInterface<CnetPoint>
    {
        public CnetInterface(CnetClient cnetClient, int pollingTimeSpan, IEnumerable<CnetPoint> points) : base(pollingTimeSpan, points)
        {
            CnetClient = cnetClient;
        }

        public CnetClient CnetClient { get; }

        public override event PollingCompletedEventHandler PollingCompleted;

        protected override void OnCreatePollingRequests()
        {
            throw new NotImplementedException();
        }

        protected override void OnPoll()
        {
            throw new NotImplementedException();
        }
    }
}
