namespace Lykke.Job.BlockchainLoadTesting.Models
{
    public class RegistrationsTestRequest
    {
        public int NewCustomersCount { get; set; }

        public int ThreadsCount { get; set; }

        public string EmailsPrefix { get; set; }
    }
}
