using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.BlockchainLoadTesting.Settings.JobSettings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }
    }
}
