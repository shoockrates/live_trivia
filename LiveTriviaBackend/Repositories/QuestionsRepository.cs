using live_trivia.Data;
using Microsoft.EntityFrameworkCore;
using live_trivia.Dtos;

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
            string normalized = NormalizeCategoryName(category);
            return await _context.Questions
                .Where(q => EF.Functions.ILike(q.Category, normalized))
                .ToListAsync();
        }

        public async Task<List<Question>> GetRandomQuestionsAsync(int count, string? category, string? difficulty)
        {
            var query = _context.Questions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                string normalizedCategory = NormalizeCategoryName(category);
                query = query.Where(q => EF.Functions.ILike(q.Category, normalizedCategory));
            }

            // Only filter by difficulty if it's explicitly provided and not "any"
            if (!string.IsNullOrWhiteSpace(difficulty) && difficulty.ToLower() != "any")
            {
                query = query.Where(q => EF.Functions.ILike(q.Difficulty, difficulty));
            }

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

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _context.Questions
                .Select(q => q.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<QuestionBankImportResultDto> ImportQuestionBankAsync(QuestionBankImportDto dto)
        {
            // load existing texts once
            var existingTexts = await _context.Questions
                .Select(q => q.Text)
                .ToListAsync();

            var existingSet = existingTexts.ToHashSet(StringComparer.OrdinalIgnoreCase);

            int total = dto.Questions.Count;
            int invalid = 0;
            int duplicates = 0;

            var toAdd = new List<Question>();

            foreach (var q in dto.Questions)
            {
                // basic validation
                if (string.IsNullOrWhiteSpace(q.Text) ||
                    q.Answers == null || q.Answers.Count < 2 ||
                    q.CorrectAnswerIndexes == null || q.CorrectAnswerIndexes.Count == 0 ||
                    q.CorrectAnswerIndexes.Any(i => i < 0 || i >= q.Answers.Count))
                {
                    invalid++;
                    continue;
                }

                if (existingSet.Contains(q.Text))
                {
                    duplicates++;
                    continue;
                }

                existingSet.Add(q.Text);

                toAdd.Add(new Question
                {
                    Text = q.Text.Trim(),
                    Answers = q.Answers,
                    CorrectAnswerIndexes = q.CorrectAnswerIndexes,
                    Category = string.IsNullOrWhiteSpace(q.Category) ? "Any" : q.Category.Trim(),
                    Difficulty = string.IsNullOrWhiteSpace(q.Difficulty) ? "medium" : q.Difficulty.Trim(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (toAdd.Count > 0)
            {
                _context.Questions.AddRange(toAdd);
                await _context.SaveChangesAsync();
            }

            return new QuestionBankImportResultDto
            {
                Total = total,
                Added = toAdd.Count,
                SkippedDuplicates = duplicates,
                SkippedInvalid = invalid
            };
        }
    }
}
