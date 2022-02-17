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

        private string FailureUrl => $"{_configuration.GetValue<string>("PaymentPortalUrl")}{_configuration.GetValue<string>("PaymentPortalFailureEndpoint")}";

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
                
                return Redirect(FailureUrl);
            }
        }

        [HttpGet("PaymentResponse/{id}")]
        public async Task<IActionResult> PaymentResponse(string id)
        {
            try
            {
                var processPaymentResponse = await Mediator.Send(new PaymentResponseCommand() { PaymentId = id });

                if (processPaymentResponse.Success == false)
                {
                    return Redirect(FailureUrl);
                }

                return Redirect(processPaymentResponse.RedirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, DefaultErrorMessage);

                return Redirect(FailureUrl);
            }
        }
    }
}
