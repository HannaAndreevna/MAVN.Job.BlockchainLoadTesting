using JetBrains.Annotations;
using MAVN.Job.BlockchainLoadTesting.Settings.JobSettings;
using MAVN.Service.Campaign.Client;
using MAVN.Service.CustomerManagement.Client;
using MAVN.Service.CustomerProfile.Client;
using MAVN.Service.WalletManagement.Client;
using Lykke.Sdk.Settings;

namespace MAVN.Job.BlockchainLoadTesting.Settings
{
    [UsedImplicitly]
    public class AppSettings : BaseAppSettings
    {
        public BlockchainLoadTestingJobSettings BlockchainLoadTestingJob { get; set; }

        public CampaignServiceClientSettings CampaignServiceClient { get; set; }

        public CustomerManagementServiceClientSettings CustomerManagementServiceClient { get; set; }

        public WalletManagementServiceClientSettings WalletManagementServiceClient { get; set; }

        public CustomerProfileServiceClientSettings CustomerProfileServiceClient { get; set; }
    }
}
