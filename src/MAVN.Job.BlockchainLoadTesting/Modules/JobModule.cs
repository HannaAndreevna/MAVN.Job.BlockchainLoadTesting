using Autofac;
using Common;
using JetBrains.Annotations;
using MAVN.Job.BlockchainLoadTesting.Domain.Services;
using MAVN.Job.BlockchainLoadTesting.DomainServices;
using MAVN.Job.BlockchainLoadTesting.Services;
using MAVN.Job.BlockchainLoadTesting.Settings;
using Lykke.RabbitMqBroker.Publisher;
using MAVN.Service.Campaign.Client;
using MAVN.Service.CustomerManagement.Client;
using MAVN.Service.CustomerManagement.Contract.Events;
using MAVN.Service.CustomerProfile.Client;
using MAVN.Service.WalletManagement.Client;
using Lykke.SettingsReader;
using Lykke.Sdk;
using Lykke.Sdk.Health;

namespace MAVN.Job.BlockchainLoadTesting.Modules
{
    [UsedImplicitly]
    public class JobModule : Module
    {
        private readonly AppSettings _appSettings;

        public JobModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings.CurrentValue;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterType<DataGenerator>()
                .As<IDataGenerator>()
                .SingleInstance();

            builder.RegisterType<JsonRabbitPublisher<EmailCodeVerifiedEvent>>()
                .As<IRabbitPublisher<EmailCodeVerifiedEvent>>()
                .As<IStartable>()
                .As<IStopable>()
                .SingleInstance()
                .WithParameter("connectionString", _appSettings.BlockchainLoadTestingJob.RabbitMq.ConnectionString)
                .WithParameter("exchangeName", "lykke.customer.emailcodeverified");

            builder.RegisterCampaignClient(_appSettings.CampaignServiceClient);
            builder.RegisterCustomerManagementClient(_appSettings.CustomerManagementServiceClient, null);
            builder.RegisterWalletManagementClient(_appSettings.WalletManagementServiceClient, null);
            builder.RegisterCustomerProfileClient(_appSettings.CustomerProfileServiceClient);
        }
    }
}
