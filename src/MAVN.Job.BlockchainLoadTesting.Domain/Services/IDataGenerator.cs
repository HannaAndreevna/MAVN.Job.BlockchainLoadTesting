using System.Collections.Generic;
using System.Threading.Tasks;

namespace MAVN.Job.BlockchainLoadTesting.Domain.Services
{
    public interface IDataGenerator
    {
        Task<List<string>> GenerateCustomersAsync(
            int customersCount,
            int threadsCount,
            string emailPrefix = null);
        Task VerifyCustomerAsync(List<string> customersIds, int threadsCount);
        Task SendTransfersAsync(List<string> customersIds, int threadsCount);
        //Task PerformRePaymentsAsync(List<string> customersIds, int threadsCount);
    }
}
