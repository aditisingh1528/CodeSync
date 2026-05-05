using System.Security.Claims;
using ExecutionService.DTOs;
using ExecutionService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExecutionService.Controllers
{
    [ApiController]
    [Route("api/executions")]
    [Authorize]
    public class ExecutionController : ControllerBase
    {
        private readonly IExecutionService _executionService;

        public ExecutionController(IExecutionService executionService)
            => _executionService = executionService;

        private int GetUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitExecutionJobDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, message, data) = await _executionService.SubmitAsync(GetUserId(), dto);
            if (!success) return StatusCode(503, new { message, job = data });

            return AcceptedAtAction(nameof(GetResult), new { id = data!.Id }, new { message, job = data });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetResult(int id)
        {
            var (success, message, data) = await _executionService.GetResultAsync(id, GetUserId());
            if (!success)
            {
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { message });

                return StatusCode(403, new { message });
            }

            return Ok(new { message, job = data });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var (success, message, data) = await _executionService.GetHistoryAsync(GetUserId());
            return Ok(new { message, jobs = data });
        }
    }
}
