using Lykke.SettingsReader.Attributes;

namespace MAVN.Job.BlockchainLoadTesting.Settings.JobSettings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }
    }
}
