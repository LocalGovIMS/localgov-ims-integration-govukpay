using Application.Commands;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RefundController : BaseController
    {
        private readonly ILogger<RefundController> _logger;
        private readonly IMapper _mapper;

        public RefundController(
            ILogger<RefundController> logger,
            IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> Post(RefundModel model)
        {
            try
            {
                var result = await Mediator.Send(new RefundRequestCommand()
                {
                    Refund = _mapper.Map<Application.Commands.RefundModel>(model)
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to process the refund");

                return BadRequest();
            }
        }
    }
}
