using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobController : BaseController
    {
        private readonly ILogger<JobController> _logger;

        public JobController(ILogger<JobController> logger)
        {
            _logger = logger;
        }

        [HttpGet("CleanupIncompletePayments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CleanupIncompletePayments()
        {
            try
            {
                var result = await Mediator.Send(new CleanupIncompletePaymentsCommand());

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to cleanup incomplete payments");
                
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
