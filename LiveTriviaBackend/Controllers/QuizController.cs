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
    [Route("quizzes")]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public QuizController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [HttpGet("quizzes")]
        public async Task<IActionResult> GetAllQuizzes()
        {
            var quizzes = await _quizService.GetAllQuizzes();
            return Ok(quizzes);
        }

        [HttpPost("submit-quiz")]
        public async Task<IActionResult> SubmitQuiz([FromBody] QuizDto quizDto)
        {
            if (quizDto == null)
                return BadRequest("Invalid quiz data.");

            // Bug fix: validate required fields
            if (string.IsNullOrWhiteSpace(quizDto.Name))
                return BadRequest("Quiz name is required.");

            if (quizDto.Questions == null || quizDto.Questions.Count == 0)
                return BadRequest("Quiz must have at least one question.");

            var result = await _quizService.SubmitQuiz(quizDto);
            return Ok(result);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetByQuiz(string name)
        {
            var quiz = await _quizService.GetQuizQuestions(name);
            return Ok(quiz);
        }
    }
}