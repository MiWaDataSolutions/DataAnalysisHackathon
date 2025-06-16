
using DataAnalystBackend.Shared.DataAccess.Models;
using DataAnalystBackend.Shared.Exceptions;
using DataAnalystBackend.Shared.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> StartGenerationAsync([FromQuery] Guid dataSessionId, [FromBody] string fileName)
        {
            try
            {
                await _dataSessionService.StartGeneration<string>(fileName, dataSessionId, User.Claims.First(o => o.Type == ClaimTypes.NameIdentifier).Value, (model, ea) =>
                {
                    Console.WriteLine($"Got Name: {model}");
                    return Task.CompletedTask;
                });
                return Ok();
            }
            catch (RecordNotFoundException rNFEx)
            {
                return NotFound(rNFEx.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occured while starting your Data Session");
            }
        }
    }
}
