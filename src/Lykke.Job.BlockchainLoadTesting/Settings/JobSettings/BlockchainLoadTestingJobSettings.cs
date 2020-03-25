namespace Lykke.Job.BlockchainLoadTesting.Settings.JobSettings
{
    public class BlockchainLoadTestingJobSettings
    {
        public DbSettings Db { get; set; }

        public RabbitMqSettings RabbitMq { get; set; }
    }
}
