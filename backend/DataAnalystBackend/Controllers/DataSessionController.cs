
using DataAnalystBackend.DTOs;
using DataAnalystBackend.Hubs;
using DataAnalystBackend.Shared.AgentAPIModels;
using DataAnalystBackend.Shared.DataAccess.Models;
using DataAnalystBackend.Shared.Exceptions;
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
        private readonly IDataSessionFileService _fileService;

        public DataSessionController(IDataSessionService dataSessionService, IDataSessionFileService fileService)
        {
            _dataSessionService = dataSessionService;
            _fileService = fileService;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(List<DataSessionDTO>))]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DataSessionDTO>>> Get()
        {
            try
            {
                List<DataSession> dataSessions = await _dataSessionService.GetDataSessionsAsync(User.Claims.First(o => o.Type == ClaimTypes.NameIdentifier).Value);
                List<DataSessionDTO> result = new List<DataSessionDTO>();
                foreach (DataSession dataSession in dataSessions)
                {
                    DataSessionDTO dataSessionDTO = new DataSessionDTO()
                    {
                        CreatedAt = dataSession.CreatedAt,
                        Id = dataSession.Id,
                        InitialFileHasHeaders = dataSession.InitialFileHasHeaders,
                        LastUpdatedAt = dataSession.LastUpdatedAt,
                        Name = dataSession.Name,
                        SchemaName = dataSession.SchemaName,
                        UserId = dataSession.UserId,
                        ProcessedStatus = await _fileService.GetLatestFileProcessedState(dataSession.Id)
                    };
                    result.Add(dataSessionDTO);
                }
                return Ok(result);
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
        [ProducesResponseType(200, Type = typeof(DataSessionDTO))]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DataSessionDTO>> GetById(Guid dataSessionId)
        {
            try
            {
                DataSession dataSession = await _dataSessionService.GetDataSessionAsync(dataSessionId, User.Claims.First(o => o.Type == ClaimTypes.NameIdentifier).Value);
                DataSessionDTO dataSessionDTO = new DataSessionDTO()
                {
                    CreatedAt = dataSession.CreatedAt,
                    Id = dataSession.Id,
                    InitialFileHasHeaders = dataSession.InitialFileHasHeaders,
                    LastUpdatedAt = dataSession.LastUpdatedAt,
                    Name = dataSession.Name,
                    SchemaName = dataSession.SchemaName,
                    UserId = dataSession.UserId,
                    ProcessedStatus = await _fileService.GetLatestFileProcessedState(dataSession.Id)
                };
                return Ok(dataSessionDTO);
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
