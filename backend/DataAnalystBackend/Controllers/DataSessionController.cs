
using DataAnalystBackend.DTOs;
using DataAnalystBackend.Hubs;
using DataAnalystBackend.Shared.AgentAPIModels;
using DataAnalystBackend.Shared.DataAccess.Models;
using DataAnalystBackend.Shared.Exceptions;
using DataAnalystBackend.Shared.Interfaces;
using DataAnalystBackend.Shared.Interfaces.Services;
using DataAnalystBackend.Shared.MessagingProviders.Models;
using DataAnalystBackend.Shared.MessagingProviders.Models.Enums;
using DataAnalystBackend.Shared.Services.RPC;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataAnalystBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DataSessionController : ControllerBase
    {
        private readonly IDataSessionService _dataSessionService;

        public DataSessionController(IDataSessionService dataSessionService)
        {
            _dataSessionService = dataSessionService;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(List<DataSession>))]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DataSession>>> Get()
        {
            try
            {
                return Ok(await _dataSessionService.GetDataSessionsAsync(User.Claims.First(o => o.Type == ClaimTypes.NameIdentifier).Value));
            }
            catch (RecordNotFoundException rNFEx)
            {
                return NotFound(rNFEx.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occured while getting your Data Sessions");
            }
        }

        [HttpGet("GetById")]
        [ProducesResponseType(200, Type = typeof(DataSession))]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DataSession>> GetById(Guid dataSessionId)
        {
            try
            {
                return Ok(await _dataSessionService.GetDataSessionAsync(dataSessionId, User.Claims.First(o => o.Type == ClaimTypes.NameIdentifier).Value));
            }
            catch (RecordNotFoundException rNFEx)
            {
                return NotFound(rNFEx.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occured while getting your Data Sessions");
            }
        }

        [HttpPost]
        [ProducesResponseType(201, Type = typeof(string))]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateDataSession([FromBody] DataSession dataSession)
        {
            try
            {
                return CreatedAtAction("GetById", await _dataSessionService.CreateDataSession(dataSession, User.Claims.First(o => o.Type == ClaimTypes.NameIdentifier).Value));
            }
            catch (RecordNotFoundException rNFEx)
            {
                return NotFound(rNFEx.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occured while creating your Data Session");
            }
        }

        [HttpPut]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateDataSession([FromQuery] Guid dataSessionId, [FromQuery] string dataSessionName)
        {
            try
            {
                await _dataSessionService.UpdateDataSession(dataSessionId, dataSessionName, User.Claims.First(o => o.Type == ClaimTypes.NameIdentifier).Value);
                return Ok();
            }
            catch (RecordNotFoundException rNFEx)
            {
                return NotFound(rNFEx.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occured while updating your Data Session");
            }
        }

        [HttpDelete]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteDataSession([FromQuery] Guid dataSessionId)
        {
            try
            {
                await _dataSessionService.DeleteDataSessionAsync(dataSessionId, User.Claims.First(o => o.Type == ClaimTypes.NameIdentifier).Value);
                return Ok();
            }
            catch (RecordNotFoundException rNFEx)
            {
                return NotFound(rNFEx.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occured while deleting your Data Session");
            }
        }

        [HttpPost("StartGeneration")]
        public async Task<IActionResult> StartGenerationAsync([FromQuery] Guid dataSessionId,  [FromBody] StartGenerationDto startGenerationDto)
        {
            try
            { 
                string userId = User.Claims.First(o => o.Type == ClaimTypes.NameIdentifier).Value;
                await _dataSessionService.StartGeneration<string>(startGenerationDto.Filename, dataSessionId, userId, startGenerationDto.InitialFileHasHeaders);
                return Ok();
            }
            catch (RecordNotFoundException rNFEx)
            {
                return NotFound(rNFEx.Message);
            }
            catch (DataCountMismatchException dcmEx)
            {
                return StatusCode(500, dcmEx.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occured while starting your Data Session");
            }
        }
    }
}
