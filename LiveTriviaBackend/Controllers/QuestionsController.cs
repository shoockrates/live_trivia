using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using live_trivia.Extensions;
using live_trivia.Interfaces;
using live_trivia.Dtos;
using System.Text;
using System.Text.Json;

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

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _questionService.GetCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("export")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Export()
        {
            var questions = await _questionService.GetAllAsync();

            var export = new QuestionBankExportDto
            {
                Questions = questions.Select(q => new QuestionExportDto
                {
                    Text = q.Text,
                    Answers = q.Answers,
                    CorrectAnswerIndexes = q.CorrectAnswerIndexes,
                    Category = q.Category,
                    Difficulty = q.Difficulty
                }).ToList()
            };

            var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return File(Encoding.UTF8.GetBytes(json), "application/json", "question-bank.json");
        }

        [HttpPost("import")]
        [Authorize(Roles = "Admin")]
        [RequestSizeLimit(15_000_000)]
        public async Task<IActionResult> Import([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            QuestionBankImportDto? import;
            using (var stream = file.OpenReadStream())
            {
                import = await JsonSerializer.DeserializeAsync<QuestionBankImportDto>(
                    stream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }

            if (import?.Questions == null) return BadRequest("Invalid question bank format.");

            var result = await _questionService.ImportQuestionBankAsync(import);
            return Ok(result);
        }
    }
}
