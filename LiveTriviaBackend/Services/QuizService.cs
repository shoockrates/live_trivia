namespace live_trivia.Services;
using live_trivia.Dtos;
using live_trivia.Interfaces;
using live_trivia.Data;
using Microsoft.EntityFrameworkCore;
public class QuizService : IQuizService
{
    private readonly TriviaDbContext _context;

    public QuizService(TriviaDbContext context)
    {
        _context = context;
    }

    public async Task<bool> SubmitQuiz(QuizDto quizDto)
    {
        if (quizDto == null) throw new ArgumentNullException(nameof(quizDto));

        var questions = quizDto.Questions.Select(q => new Question
        {
            Text = q.Text,
            Answers = q.Answers,
            CorrectAnswerIndexes = q.CorrectAnswerIndexes,
            Category = q.Category,
            Difficulty = q.Difficulty,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var quiz = new Quiz
        {
            Name = quizDto.Name,
            Category = quizDto.Category,
            Difficulty = quizDto.Difficulty,
            Questions = questions,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var question in quiz.Questions)
        {
            // Only add if it's a new question (no Id yet)
            if (question.Id == 0)
            {
                _context.Questions.Add(question);
            }
        }
        _context.Quiz.Add(quiz);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Question>> GetQuizQuestions(string name)
        {
            var quiz = await _context.Quiz
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Name.ToLower() == name.ToLower());

            if (quiz == null)
                return new List<Question>();

            return quiz.Questions.ToList();
        }

        public async Task<Quiz?> GetQuizByName(string name)
        {
            return await _context.Quiz
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Name.ToLower() == name.ToLower());
        }

        public async Task<List<Quiz>> GetAllQuizzes()
        {
            return await _context.Quiz
                .Include(q => q.Questions)
                .OrderBy(q => q.Name)
                .ToListAsync();
        }
}