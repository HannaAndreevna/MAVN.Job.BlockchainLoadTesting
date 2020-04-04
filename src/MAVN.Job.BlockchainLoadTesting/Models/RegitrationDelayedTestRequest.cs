using System;

namespace MAVN.Job.BlockchainLoadTesting.Models
{
    public class RegitrationDelayedTestRequest : RegistrationsTestRequest
    {
        public TimeSpan AfterVerificationIdlePeriod { get; set; }
    }
}
