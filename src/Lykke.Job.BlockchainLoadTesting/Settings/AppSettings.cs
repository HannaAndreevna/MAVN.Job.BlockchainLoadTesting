using JetBrains.Annotations;
using Lykke.Job.BlockchainLoadTesting.Settings.JobSettings;
using Lykke.Service.Campaign.Client;
using Lykke.Service.CustomerManagement.Client;
using Lykke.Service.CustomerProfile.Client;
using Lykke.Service.PaymentTransfers.Client;
using Lykke.Service.WalletManagement.Client;
using Lykke.Sdk.Settings;

namespace Lykke.Job.BlockchainLoadTesting.Settings
{
    [UsedImplicitly]
    public class AppSettings : BaseAppSettings
    {
        public BlockchainLoadTestingJobSettings BlockchainLoadTestingJob { get; set; }

        public CampaignServiceClientSettings CampaignServiceClient { get; set; }

        public CustomerManagementServiceClientSettings CustomerManagementServiceClient { get; set; }

        public PaymentTransfersServiceClientSettings PaymentTransfersServiceClient { get; set; }

        public WalletManagementServiceClientSettings WalletManagementServiceClient { get; set; }

        public CustomerProfileServiceClientSettings CustomerProfileServiceClient { get; set; }
    }
}
