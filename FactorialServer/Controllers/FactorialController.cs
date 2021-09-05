using FactorialServer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactorialServer.Controllers
{
    [ApiController]
    [Route("/public_api/")]
    public class FactorialController : ControllerBase
    {
        private readonly ILogger<FactorialController> _logger;
        private readonly IFactorialCalculator _factorialService;

        public FactorialController(ILogger<FactorialController> logger, IFactorialCalculator factorialService)
        {
            _logger = logger;
            _factorialService = factorialService;
        }

        [HttpGet]
        [Route("get_factorial/{number:long}/")]
        public async Task<IActionResult> Get(long number)
        {
            var result = await _factorialService.CalculateAsync(number);

            // TODO handle any result cases
            if (string.IsNullOrEmpty(result))
                return StatusCode(504);
            else
                return Ok(result.ToString());
        }
    }
}