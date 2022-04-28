using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Route("[controller]")]
    public class PaymentController : BaseController
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration _configuration;

        private const string DefaultErrorMessage = "Unable to process the payment";

        private string FailureUrl(string host) => $"{host}{_configuration.GetValue<string>("FailureEndpoint")}";

        public PaymentController(
            ILogger<PaymentController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("{reference}/{hash}")]
        public async Task<IActionResult> Index(string reference, string hash)
        {
            try
            {
                var result = await Mediator.Send(
                    new CreatePaymentRequestCommand()
                    {
                        Reference = reference,
                        Hash = hash
                    });
                
                return Redirect(result.NextUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, DefaultErrorMessage);

                return await DetermineFailureUrl(reference);
            }
        }

        private async Task<IActionResult> DetermineFailureUrl(string reference)
        {
            var paymentFailureUrl = await GetPaymentFailureUrl(reference);

            if (!string.IsNullOrEmpty(paymentFailureUrl))
            {
                return Redirect(paymentFailureUrl);
            }

            return ReturnToCaller();
        }

        private async Task<string> GetPaymentFailureUrl(string reference)
        {
            return (await Mediator.Send(new GetPaymentCommand() { Reference = reference })).Payment?.FailureUrl;
        }

        private IActionResult ReturnToCaller()
        {
            Request.Headers.TryGetValue("Referer", out var host);

            return Redirect(FailureUrl(host));
        }

        [HttpGet("PaymentResponse/{id}")]
        public async Task<IActionResult> PaymentResponse(string id)
        {
            try
            {
                var processPaymentResponse = await Mediator.Send(new PaymentResponseCommand() { PaymentId = id });

                return Redirect(processPaymentResponse.NextUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, DefaultErrorMessage);

                var response = await Mediator.Send(new GetPaymentCommand() { Id = id });

                return Redirect(response.Payment.FailureUrl);
            }
        }
    }
}
