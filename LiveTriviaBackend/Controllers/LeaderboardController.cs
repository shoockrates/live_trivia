using live_trivia.Data;
using live_trivia.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace live_trivia.Controllers
{
    [ApiController]
    [Route("leaderboard")]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardService _leaderboardService;
        private readonly TriviaDbContext _context;

        public LeaderboardController(ILeaderboardService leaderboardService, TriviaDbContext context)
        {
            _leaderboardService = leaderboardService;
            _context = context;
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopPlayers([FromQuery] int top = 10)
        {
            if (top < 1 || top > 100)
            {
                return BadRequest("Top count must be between 1 and 100");
            }

            var topPlayers = await _leaderboardService.GetTopPlayersAsync(top);
            return Ok(topPlayers);
        }

        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetTopPlayersByCategory(string category, [FromQuery] int top = 10)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return BadRequest("Category is required");
            }

            if (top < 1 || top > 100)
            {
                return BadRequest("Top count must be between 1 and 100");
            }

            var topPlayers = await _leaderboardService.GetTopPlayersByCategoryAsync(category, top);
            return Ok(topPlayers);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetAvailableCategories()
        {
            var categories = await _leaderboardService.GetAvailableCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("global-stats")]
        public async Task<IActionResult> GetGlobalStats()
        {
            var totalPlayers = await _context.PlayerStatistics.CountAsync(ps => ps.TotalGamesPlayed > 0);
            var totalGames = await _context.PlayerStatistics.SumAsync(ps => ps.TotalGamesPlayed);
            var averageAccuracy = await _context.PlayerStatistics
                .Where(ps => ps.TotalQuestionsAnswered > 0)
                .AverageAsync(ps => (double)ps.TotalCorrectAnswers / ps.TotalQuestionsAnswered * 100);

            return Ok(new
            {
                TotalPlayers = totalPlayers,
                TotalGames = totalGames,
                AverageAccuracy = Math.Round(averageAccuracy, 2)
            });
        }
    }
}
