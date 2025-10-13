using Microsoft.AspNetCore.Mvc;
using live_trivia.Repositories;

namespace LiveTriviaBackend.Controllers
{
    [ApiController]
    [Route("questions")]
    public class QuestionsController : ControllerBase
    {
        private readonly QuestionsRepository _repository;

        public QuestionsController(QuestionsRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuestions()
        {
            var questions = await _repository.GetAllAsync();
            return Ok(questions);
        }

        [HttpGet("random")]
        public async Task<IActionResult> GetRandom()
        {
            var question = await _repository.GetRandomAsync();
            if (question == null) return NotFound("No questions available.");
            return Ok(question);
        }

        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var questions = await _repository.GetByCategoryAsync(category);
            return Ok(questions);
        }

        [HttpPost("load/{file}")]
        public async Task<IActionResult> LoadFromFile(string file)
        {
            try
            {
                var addedCount = await _repository.LoadFromFileAsync(file);
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
