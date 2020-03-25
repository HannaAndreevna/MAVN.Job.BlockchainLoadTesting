using System;

namespace Lykke.Job.BlockchainLoadTesting.Models
{
    public class RegitrationDelayedTestRequest : RegistrationsTestRequest
    {
        public TimeSpan AfterVerificationIdlePeriod { get; set; }
    }
}
