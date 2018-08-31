using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RCON_API.Helpers;
using RCON_API.Models;

namespace RCON_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PodsController : ControllerBase
    {

        private readonly KubeHelper _client;

        public PodsController(KubeHelper client)
        {
            _client = client;
        }

        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<MinecraftPod>> Get()
        {
            return _client.GetServicePods();
        }

        [HttpPost]
        public ActionResult<MinecraftPod> Create([FromBody] string podName)
        {
            return _client.AddServicePod(podName);
        }
    }
}