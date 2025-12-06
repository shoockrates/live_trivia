using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using live_trivia.Interfaces;
using live_trivia.Dtos;

namespace live_trivia.Controllers
{
    [ApiController]
    [Route("statistics")]
    [Authorize]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet("player")]
        public async Task<IActionResult> GetPlayerStatistics()
        {
            var playerIdClaim = User.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                return Unauthorized("Player identity not found.");
            }

            var stats = await _statisticsService.GetPlayerStatisticsAsync(playerId);
            return Ok(stats);
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateStatistics([FromBody] UpdateStatsRequest request)
        {
            var playerIdClaim = User.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                return Unauthorized("Player identity not found.");
            }

            await _statisticsService.UpdateGameStatisticsAsync(
                playerId,
                request.Category,
                request.Score,
                request.CorrectAnswers,
                request.TotalQuestions
            );

            return Ok("Statistics updated successfully");
        }
    }
}
