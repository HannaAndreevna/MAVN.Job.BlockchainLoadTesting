using System.Net;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.BlockchainLoadTesting.Domain.Services;
using Lykke.Job.BlockchainLoadTesting.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Job.BlockchainLoadTesting.Controllers
{
    [Route("api/loadtest")]
    public class LoadTestController : ControllerBase
    {
        private readonly IDataGenerator _dataGenerator;
        private readonly ILog _log;

        public LoadTestController(IDataGenerator dataGenerator, ILogFactory logFactory)
        {
            _dataGenerator = dataGenerator;
            _log = logFactory.CreateLog(this);
        }

        /// <summary>
        /// Runs registrations load test.
        /// </summary>
        [HttpPost("registrations")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task RunRegistrationsTestAsync(RegistrationsTestRequest request)
        {
            await _dataGenerator.GenerateCustomersAsync(
                request.NewCustomersCount,
                request.ThreadsCount,
                request.EmailsPrefix);
        }

        /// <summary>
        /// Runs registrations with verification load test.
        /// </summary>
        [HttpPost("registrations-with-verification")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task RunRegistrationsWithVerificationTestAsync(RegistrationsTestRequest request)
        {
            var customerIds = await _dataGenerator.GenerateCustomersAsync(
                request.NewCustomersCount,
                request.ThreadsCount,
                request.EmailsPrefix);
            await _dataGenerator.VerifyCustomerAsync(customerIds, request.ThreadsCount);
        }

        /// <summary>
        /// Runs registrations with email verification and p2p tarnsfers load test.
        /// </summary>
        [HttpPost("transfers")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task RunTransfersTestAsync(RegitrationDelayedTestRequest request)
        {
            var customerIds = await _dataGenerator.GenerateCustomersAsync(
                request.NewCustomersCount,
                request.ThreadsCount,
                request.EmailsPrefix);
            await _dataGenerator.VerifyCustomerAsync(customerIds, request.ThreadsCount);

            _log.Info($"Waiting for {request.AfterVerificationIdlePeriod}");
            await Task.Delay(request.AfterVerificationIdlePeriod);

            await _dataGenerator.SendTransfersAsync(customerIds, request.ThreadsCount);
        }

        /// <summary>
        /// Runs registrations with email verification and p2p tarnsfers load test.
        /// </summary>
        //[HttpPost("re-payments")]
        //[ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        //public async Task RunRePaymentsTestAsync(RegitrationDelayedTestRequest request)
        //{
        //    var customerIds = await _dataGenerator.GenerateCustomersAsync(
        //        request.NewCustomersCount,
        //        request.ThreadsCount,
        //        request.EmailsPrefix);
        //    await _dataGenerator.VerifyCustomerAsync(customerIds, request.ThreadsCount);

        //    _log.Info($"Waiting for {request.AfterVerificationIdlePeriod}");
        //    await Task.Delay(request.AfterVerificationIdlePeriod);

        //    await _dataGenerator.PerformRePaymentsAsync(customerIds, request.ThreadsCount);
        //}
    }
}
