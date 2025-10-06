using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using live_trivia.Data;
using live_trivia;

namespace LiveTriviaBackend.Controllers
{
    [ApiController]
    [Route("questions")]

    public class QuestionsController : ControllerBase
    {
        private readonly TriviaDbContext _context;

        public QuestionsController(TriviaDbContext context)
        {
            _context = context;
        }

        
        [HttpGet("random")]
        public async Task<IActionResult> GetRandom()
        {
            var count = await _context.Questions.CountAsync();
            if (count == 0) return NotFound("No questions available.");

            var rand = new Random(); // consider making Random static to avoid repeated seed problems
            var index = rand.Next(count);

            var question = await _context.Questions.Skip(index).FirstOrDefaultAsync();
            if (question == null) return NotFound();

            return Ok(question);
        }


        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var questions = await _context.Questions
                .Where(q => EF.Functions.ILike(q.Category, category)) // Postgres ILIKE
                .ToListAsync();
            return Ok(questions);
        }

        [HttpPost("load/{file}")]
        public async Task<IActionResult> LoadFromFile(string file)
        {
            if (!System.IO.File.Exists(file))
                return NotFound($"File {file} not found.");

            var questionBank = new QuestionBank(file);
            var newQuestions = questionBank.Questions
                .Where(q => !_context.Questions.Any(e => e.Text == q.Text))
                .ToList();

            if (!newQuestions.Any())
                return Ok("No new questions to add.");

            _context.Questions.AddRange(newQuestions);
            await _context.SaveChangesAsync();

            return Ok($"{newQuestions.Count} questions loaded from {file}");
        }
    }
}
