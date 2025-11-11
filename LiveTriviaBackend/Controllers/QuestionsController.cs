using Microsoft.AspNetCore.Mvc;
using live_trivia.Repositories;
using Microsoft.AspNetCore.Authorization;
using live_trivia.Extensions;
using live_trivia.Services;
using live_trivia.Interfaces;

namespace LiveTriviaBackend.Controllers
{
    [ApiController]
    [Route("questions")]
    public class QuestionsController : ControllerBase
    {
        private readonly IQuestionService _questionService;

        public QuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuestions()
        {
            var questions = await _questionService.GetAllAsync();
            return Ok(questions);
        }

        [HttpGet("random")]
        public async Task<IActionResult> GetRandom()
        {
            var question = await _questionService.GetRandomAsync();
            if (question == null) return NotFound("No questions available.");
            return Ok(question);
        }

        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var questions = await _questionService.GetByCategoryAsync(category.ToLower().CapitalizeFirstLetter());
            return Ok(questions);
        }

        // TODO: Perhaps restrict to admin users only
        [Authorize]
        [HttpPost("load/{file}")]
        public async Task<IActionResult> LoadFromFile(string file)
        {
            try
            {
                var addedCount = await _questionService.LoadFromFileAsync(file);
                if (addedCount == 0)
                    return Ok("No new questions to add.");
                return Ok($"{addedCount} questions loaded from {file}");
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
