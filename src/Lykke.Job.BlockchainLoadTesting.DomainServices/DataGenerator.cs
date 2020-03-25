using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.BlockchainLoadTesting.Domain.Services;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.Service.Campaign.Client;
using Lykke.Service.Campaign.Client.Models.BurnRule.Requests;
using Lykke.Service.CustomerManagement.Client;
using Lykke.Service.CustomerManagement.Client.Models.Requests;
using Lykke.Service.CustomerManagement.Contract.Events;
using Lykke.Service.CustomerProfile.Client;
using Lykke.Service.CustomerProfile.Client.Models.Enums;
using Lykke.Service.CustomerProfile.Client.Models.Requests;
using Lykke.Service.PartnerManagement.Client.Models;
using Lykke.Service.PaymentTransfers.Client;
using Lykke.Service.PaymentTransfers.Client.Models.Requests;
using Lykke.Service.WalletManagement.Client;
using Lykke.Service.WalletManagement.Client.Enums;
using Lykke.Service.WalletManagement.Client.Models.Requests;

namespace Lykke.Job.BlockchainLoadTesting.DomainServices
{
    public class DataGenerator : IDataGenerator
    {
        private readonly ICampaignClient _campaignClient;
        private readonly ICustomerManagementServiceClient _customerManagementServiceClient;
        private readonly IPaymentTransfersClient _paymentTransfersClient;
        private readonly IWalletManagementClient _walletManagementClient;
        private readonly ICustomerProfileClient _customerProfileClient;
        private readonly IRabbitPublisher<EmailCodeVerifiedEvent> _emailVerifiedPublisher;
        private readonly ILog _log;

        public DataGenerator(
            ICampaignClient campaignClient,
            ICustomerManagementServiceClient customerManagementServiceClient,
            IPaymentTransfersClient paymentTransfersClient,
            IWalletManagementClient walletManagementClient,
            ICustomerProfileClient customerProfileClient,
            IRabbitPublisher<EmailCodeVerifiedEvent> emailVerifiedPublisher,
            ILogFactory logFactory)
        {
            _campaignClient = campaignClient;
            _customerManagementServiceClient = customerManagementServiceClient;
            _paymentTransfersClient = paymentTransfersClient;
            _walletManagementClient = walletManagementClient;
            _customerProfileClient = customerProfileClient;
            _emailVerifiedPublisher = emailVerifiedPublisher;
            _log = logFactory.CreateLog(this);
        }

        public Task<List<string>> GenerateCustomersAsync(
            int customersCount,
            int threadsCount,
            string emailPrefix = null)
        {
            return RunResultOperationsBatchAsync(
                customersCount,
                threadsCount,
                "customers creation",
                (x, y) => ThreadGenerateCustomersAsync(x, y, emailPrefix));
        }

        public Task VerifyCustomerAsync(List<string> customersIds, int threadsCount)
        {
            return RunOperationsBatchAsync(
                customersIds,
                threadsCount,
                "customer verification",
                ThreadCustomerVerificationAsync);
        }

        public Task SendTransfersAsync(List<string> customersIds, int threadsCount)
        {
            return RunOperationsBatchAsync(
                customersIds,
                threadsCount,
                "send transfers",
                ThreadSendTransfersAsync);
        }

        // TODO need to rework RE payments part according to new flow with SalesForce
        //public async Task PerformRePaymentsAsync(List<string> customersIds, int threadsCount)
        //{
        //    var burnRules = await _campaignClient.BurnRules.GetAsync(
        //        new BurnRulePaginationRequest
        //        {
        //            CurrentPage = 1,
        //            PageSize = 100
        //        });
        //    var campaignId = burnRules.BurnRules.First(b => b.Vertical == Vertical.RealEstate).Id;

        //    await RunOperationsBatchAsync(
        //        customersIds,
        //        threadsCount,
        //        "send re payments",
        //        (i, l) => ThreadPerformRePaymentsAsync(i, l, campaignId.ToString()));

        //    var testRequests = new List<string>(customersIds.Count);
        //    var currentPage = 1;
        //    while(true)
        //    {
        //        var requests = _paymentTransfersClient.Api.PaymentTransferAsync().GetUnprocessedPaymentTransfersAsync(
        //            new PaginatedRequest { CurrentPage = currentPage, PageSize = 100 });
        //        if (!requests.Result.PaymentTransfers?.Any() ?? true)
        //            break;

        //        testRequests.AddRange(
        //            requests.Result.PaymentTransfers
        //                .Where(p => IsTestGeneratedPaymentRequest(p.InvoiceId))
        //                .Select(p => p.TransferId));

        //        ++currentPage;
        //    }

        //    await RunOperationsBatchAsync(
        //        testRequests,
        //        threadsCount,
        //        "accept payments",
        //        ThreadAcceptPaymentsAsync);
        //}

        //private bool IsTestGeneratedPaymentRequest(string invoiceId)
        //{
        //    //InvoiceId = $"{DateTime.UtcNow:yyMMddHHmmssfff}-{threadNumber}",
        //    var parts = invoiceId.Split('-');
        //    if (parts.Length != 2
        //        || !int.TryParse(parts[1], out _)
        //        || !DateTime.TryParseExact(parts[0], "yyMMddHHmmssfff", null, DateTimeStyles.None, out _))
        //        return false;

        //    return true;
        //}

        private async Task<List<TOut>> RunResultOperationsBatchAsync<TOut>(
            int itemsCount,
            int threadsMaxCount,
            string operationBatchName,
            Func<int, int, Task<List<TOut>>> operationFunc)
        {
            _log.Info($"Started {operationBatchName} with {itemsCount} items");

            threadsMaxCount = Math.Min(itemsCount, threadsMaxCount);
            var threadsLoad = new int[threadsMaxCount];
            int aveThreadLoad = itemsCount / threadsMaxCount;
            for (int i = 0; i < threadsMaxCount; i++)
            {
                int currentThreadLoad = 2 * aveThreadLoad <= itemsCount ? aveThreadLoad : itemsCount;
                threadsLoad[i] = currentThreadLoad;
                itemsCount -= currentThreadLoad;
            }

            var tasks = new List<Task<List<TOut>>>(threadsMaxCount);
            for (int i = 0; i < threadsMaxCount; i++)
            {
                tasks.Add(operationFunc(i, threadsLoad[i]));
            }
            await Task.WhenAll(tasks);

            _log.Info($"{operationBatchName} finished");

            return tasks.Select(t => t.Result).SelectMany(i => i).ToList();
        }

        private async Task RunOperationsBatchAsync(
            List<string> customerIds,
            int threadsMaxCount,
            string operationBatchName,
            Func<int, List<string>, Task> operationFunc)
        {
            var itemsCount = customerIds.Count;

            _log.Info($"Started {operationBatchName} with {itemsCount} items");

            threadsMaxCount = Math.Min(itemsCount, threadsMaxCount);
            var threadsLoad = new int[threadsMaxCount];
            int aveThreadLoad = itemsCount / threadsMaxCount;
            for (int i = 0; i < threadsMaxCount; i++)
            {
                int currentThreadLoad = 2 * aveThreadLoad <= itemsCount ? aveThreadLoad : itemsCount;
                threadsLoad[i] = currentThreadLoad;
                itemsCount -= currentThreadLoad;
            }

            var tasks = new List<Task>(threadsMaxCount);
            int shift = 0;
            for (int i = 0; i < threadsMaxCount; i++)
            {
                tasks.Add(operationFunc(i, customerIds.GetRange(shift, threadsLoad[i])));
                shift += threadsLoad[i];
            }
            await Task.WhenAll(tasks);

            _log.Info($"Finished {operationBatchName}");
        }

        private async Task<List<string>> ThreadGenerateCustomersAsync(
            int threadNumber,
            int threadLoad,
            string emailPrefix = null)
        {
            var result = new List<string>(threadLoad);

            for (int i = 0; i < threadLoad; i++)
            {
                try
                {
                    var email = string.IsNullOrWhiteSpace(emailPrefix)
                        ? $"mail{threadNumber}t{DateTime.UtcNow:yyyyMMddHHmmssfff}@test.com"
                        : $"{emailPrefix}+{i}@test.com";
                    var resp = await _customerManagementServiceClient.CustomersApi.RegisterAsync(
                        new RegistrationRequestModel
                        {
                            FirstName = "TestName",
                            LastName = "TestSurname",
                            CountryOfNationalityId = 1,
                            Email = email,
                            Password = "Password1!",
                        });
                    if (resp.Error != CustomerManagementError.None)
                        throw new InvalidOperationException($"CM returned {resp.Error} for email {emailPrefix}");
                    result.Add(resp.CustomerId);
                }
                catch (Exception e)
                {
                    _log.Error(e);
                }
            }

            return result;
        }

        private async Task ThreadCustomerVerificationAsync(int threadNumber, List<string> customerIds)
        {
            var phoneNumberGen = new Random((int)DateTime.UtcNow.Ticks);

            foreach (var customerId in customerIds)
            {
                try
                {
                    await _emailVerifiedPublisher.PublishAsync(
                        new EmailCodeVerifiedEvent
                        {
                            CustomerId = customerId,
                            TimeStamp = DateTime.UtcNow,
                        });

                    await Task.Delay(100);

                    while (true)
                    {
                        var phoneNumerStr = (10000000000 * phoneNumberGen.NextDouble()).ToString().Substring(0, 10);
                        var dotIndex = phoneNumerStr.IndexOfAny(new[] {'.', ','});
                        if (dotIndex != -1)
                            phoneNumerStr = phoneNumerStr.Substring(0, dotIndex);

                        try
                        {
                            await _customerProfileClient.CustomerPhones.SetCustomerPhoneInfoAsync(
                                new SetCustomerPhoneInfoRequestModel
                                {
                                    CustomerId = customerId,
                                    PhoneNumber = phoneNumerStr,
                                    CountryPhoneCodeId = 1,
                                });
                        }
                        catch (Exception e)
                        {
                            _log.Error(e, context: phoneNumerStr);
                            continue;
                        }

                        var verificationResult = await _customerProfileClient.CustomerPhones.SetCustomerPhoneAsVerifiedAsync(
                            new SetPhoneAsVerifiedRequestModel
                            {
                                CustomerId = customerId
                            });

                        if (verificationResult.ErrorCode == CustomerProfileErrorCodes.None)
                            break;
                    }
                }
                catch (Exception e)
                {
                    _log.Error(e);
                }
            }
        }

        private async Task ThreadSendTransfersAsync(int threadNumber, List<string> customerIds)
        {
            if (customerIds.Count < 2)
                throw new InvalidOperationException("There must be at least 2 customers per thread for transfers testing");

            for (var i = 0; i < customerIds.Count; i++)
            {
                var customerId = customerIds[i];
                var receiverCustomerId = customerIds[(i + 1) % customerIds.Count];
                try
                {
                    var resp = await _walletManagementClient.Api.TransferBalanceAsync(
                        new TransferBalanceRequestModel
                        {
                            SenderCustomerId = customerId,
                            ReceiverCustomerId = receiverCustomerId,
                            Amount = 1,
                            OperationId = Guid.NewGuid().ToString(),
                        });
                    if (resp.ErrorCode != TransferErrorCodes.None)
                        throw new InvalidOperationException($"Transfer failed: {resp.ErrorCode}");
                }
                catch (Exception e)
                {
                    _log.Error(e, context: new { customerId, receiverCustomerId });
                }
            }
        }

        //private async Task ThreadPerformRePaymentsAsync(
        //    int threadNumber,
        //    List<string> customerIds,
        //    string campaignId)
        //{
        //    foreach (var customerId in customerIds)
        //    {
        //        try
        //        {
        //            await _walletManagementClient.Api.PaymentTransferAsync(
        //                new PaymentTransferRequestModel
        //                {
        //                    CustomerId = customerId,
        //                    Amount = 1,
        //                    CampaignId = campaignId,
        //                    InvoiceId = $"{DateTime.UtcNow:yyMMddHHmmssfff}-{threadNumber}",
        //                });
        //        }
        //        catch (Exception e)
        //        {
        //            _log.Error(e);
        //        }
        //    }
        //}

        //private async Task ThreadAcceptPaymentsAsync(int threadNumber, List<string> paymentRequestIds)
        //{
        //    foreach (var paymentRequestId in paymentRequestIds)
        //    {
        //        try
        //        {
        //            await _paymentTransfersClient.Api.AcceptPaymentTransferAsync(paymentRequestId);
        //        }
        //        catch (Exception e)
        //        {
        //            _log.Error(e);
        //        }
        //    }
        //}
    }
}
