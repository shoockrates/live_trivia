using live_trivia.Data;
using Microsoft.EntityFrameworkCore;

namespace live_trivia.Repositories
{
    public class QuestionsRepository
    {
        private readonly TriviaDbContext _context;

        public QuestionsRepository(TriviaDbContext context)
        {
            _context = context;
        }

        // Get all questions
        public async Task<List<Question>> GetAllAsync()
        {
            return await _context.Questions.ToListAsync();
        }

        // Get a random question
        public async Task<Question?> GetRandomAsync()
        {
            var count = await _context.Questions.CountAsync();
            if (count == 0) return null;

            var rand = new Random();
            int index = rand.Next(count);

            return await _context.Questions.Skip(index).FirstOrDefaultAsync();
        }

        // Get questions by category (case-insensitive)
        public async Task<List<Question>> GetByCategoryAsync(string category)
        {
<<<<<<< HEAD
=======
            string normalized = NormalizeCategoryName(category);
>>>>>>> b1bc474 (Fixed Arts & Literature category not working)
            return await _context.Questions
                .Where(q => q.Category == category)
                .ToListAsync();
        }

        public async Task<List<Question>> GetRandomQuestionsAsync(int count, string? category, string? difficulty)
        {
            var query = _context.Questions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
<<<<<<< HEAD
                // Use case-insensitive matching for category
                query = query.Where(q => EF.Functions.ILike(q.Category, category));
=======
                string normalizedCategory = NormalizeCategoryName(category);
                query = query.Where(q => q.Category == normalizedCategory);
>>>>>>> b1bc474 (Fixed Arts & Literature category not working)
            }

            // Only filter by difficulty if it's explicitly provided and not "any"
            if (!string.IsNullOrWhiteSpace(difficulty) && difficulty.ToLower() != "any")
            {
                query = query.Where(q => EF.Functions.ILike(q.Difficulty, difficulty));
<<<<<<< HEAD
            }
=======
>>>>>>> b1bc474 (Fixed Arts & Literature category not working)

            return await query
                .OrderBy(q => EF.Functions.Random())
                .Take(count)
                .ToListAsync();
        }

        // Helper method to normalize category names
        private string NormalizeCategoryName(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return category;

            // Convert to lowercase and trim
            category = category.Trim().ToLower();

            // Capitalize first letter of each word for general cases
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(category);
        }

        // Load new questions from file
        public async Task<int> LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File {filePath} not found.");

            var questionBank = new QuestionBank(filePath);
            var newQuestions = questionBank.Questions
                .Where(q => !_context.Questions.Any(e => e.Text == q.Text))
                .ToList();

            if (newQuestions.Count == 0)
                return 0;

            _context.Questions.AddRange(newQuestions);
            await _context.SaveChangesAsync();

            return newQuestions.Count;
        }
    }
}
