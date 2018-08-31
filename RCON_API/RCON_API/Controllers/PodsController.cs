using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest;
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
        public async Task<ActionResult<IEnumerable<MinecraftPod>>> Get()
        {
            return await _client.GetServicePods();
        }

        [HttpPost]
        public ActionResult Create([FromBody] string podName)
        {
            if (string.IsNullOrEmpty(podName)) return BadRequest("PodName is null or empty");
            try
            {
                _client.AddServicePod(podName);
            }
            catch (HttpOperationException httpEx)
            {
                return BadRequest(httpEx.Response.Content);
            }            
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
            return Ok();
        }


        [HttpDelete]
        public ActionResult Delete([FromBody] string podName)
        {
            if (string.IsNullOrEmpty(podName)) return BadRequest("PodName is null or empty");
            _client.DeleteServicePod(podName);
            return Ok();
        }
    }
}