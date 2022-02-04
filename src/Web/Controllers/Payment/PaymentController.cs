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
                var result = await Mediator.Send(new CreatePaymentRequestCommand() { Reference = reference, Hash = hash });

                return Redirect(result.NextUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, DefaultErrorMessage);

                ViewBag.ExMessage = DefaultErrorMessage;
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [HttpGet("PaymentResponse/{id}")]
        public async Task<IActionResult> PaymentResponse(string id)
        {
            try
            {
                var processPaymentResponse = await Mediator.Send(new PaymentResponseCommand() { PaymentId = id });

                if (!processPaymentResponse.Success)
                {
                    ViewBag.ExMessage = DefaultErrorMessage;
                    return View("~/Views/Shared/Error.cshtml");
                }

                return Redirect(processPaymentResponse.RedirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, DefaultErrorMessage);

                ViewBag.ExMessage = DefaultErrorMessage;
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
