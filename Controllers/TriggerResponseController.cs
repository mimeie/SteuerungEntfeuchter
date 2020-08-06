using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SteuerungEntfeuchter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TriggerResponseController : ControllerBase
    {
        
        private readonly ILogger<TriggerResponseController> _logger;

        public TriggerResponseController(ILogger<TriggerResponseController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public TriggerResponse Get()
        {
            SteuerungLogic.Instance.Update();

            Console.WriteLine("getter");
            return new TriggerResponse
            {
                ReturnCode = 1,
                Date = DateTime.Now,
                Host = System.Environment.MachineName
            };             
                        
        }
    }
}
