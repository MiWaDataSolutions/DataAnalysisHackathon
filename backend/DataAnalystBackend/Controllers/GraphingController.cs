using DataAnalystBackend.DTOs;
using DataAnalystBackend.Shared.Interfaces.Services;
using DataAnalystBackend.Shared.Interfaces.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DataAnalystBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GraphingController : ControllerBase
    {
        private readonly IGraphingDataService _graphingDataService;

        public GraphingController(IGraphingDataService graphingDataService)
        {
            _graphingDataService = graphingDataService;
        }

        [HttpGet("GetKPIGraphs")]
        [ProducesResponseType(200, Type = typeof(List<KPIModel>))]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<KPIModel>>> GetKPIGraphs(Guid dataSessionId)
        {
            return Ok(await _graphingDataService.GetKPIs(User.Claims.First(o => o.Type == ClaimTypes.NameIdentifier).Value, dataSessionId));
        }

        [HttpGet("GetGraphs")]
        [ProducesResponseType(200, Type = typeof(Dictionary<string, List<GraphModel>>))]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Dictionary<string, List<GraphModel>>>> GetGraphs(Guid dataSessionId)
        {
            return Ok(await _graphingDataService.GetGraphs(User.Claims.First(o => o.Type == ClaimTypes.NameIdentifier).Value, dataSessionId));
        }
    }
}
