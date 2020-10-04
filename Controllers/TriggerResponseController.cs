using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using JusiBase;

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
        public ResponseTrigger Get()
        {
            SteuerungLogic.Instance.Update();

            Console.WriteLine("getter");
            return new ResponseTrigger
            {
                ReturnCode = 1,
                ReturnState = SteuerungLogic.Instance.CurrentState.ToString()
            };             
                        
        }
    }
}
