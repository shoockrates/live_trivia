using Xunit;
using live_trivia.Services;
using live_trivia.Repositories;
using live_trivia.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace live_trivia.Tests
{
    public class QuestionServiceTests
    {
        private TriviaDbContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<TriviaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TriviaDbContext(options);
        }

        private QuestionsRepository CreateRepo(TriviaDbContext db)
        {
            return new QuestionsRepository(db);
        }

        private QuestionService CreateService(TriviaDbContext db)
        {
            return new QuestionService(CreateRepo(db));
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllQuestions()
        {
            var db = GetInMemoryDb();
            db.Questions.AddRange(
                new Question { Text = "Q1", Category = "Math", Difficulty = "Easy" },
                new Question { Text = "Q2", Category = "Science", Difficulty = "Medium" }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.GetAllAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, q => q.Text == "Q1");
            Assert.Contains(result, q => q.Text == "Q2");
        }

        [Fact]
        public async Task GetRandomAsync_ShouldReturnQuestionOrNull()
        {
            var db = GetInMemoryDb();
            db.Questions.AddRange(
                new Question { Text = "Q1", Category = "Math", Difficulty = "Easy" }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.GetRandomAsync();

            Assert.NotNull(result);
            Assert.Equal("Q1", result!.Text);

            // Test empty DB returns null
            var emptyDb = GetInMemoryDb();
            var emptyService = CreateService(emptyDb);
            var emptyResult = await emptyService.GetRandomAsync();
            Assert.Null(emptyResult);
        }

        [Fact]
        public async Task GetByCategoryAsync_ShouldReturnOnlyMatchingCategory()
        {
            var db = GetInMemoryDb();
            db.Questions.AddRange(
                new Question { Text = "Q1", Category = "Math", Difficulty = "Easy" },
                new Question { Text = "Q2", Category = "Science", Difficulty = "Medium" }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var mathQuestions = await service.GetByCategoryAsync("Math");

            Assert.Single(mathQuestions);
            Assert.Equal("Q1", mathQuestions.First().Text);

            var scienceQuestions = await service.GetByCategoryAsync("Science");
            Assert.Single(scienceQuestions);
            Assert.Equal("Q2", scienceQuestions.First().Text);

            var emptyCategory = await service.GetByCategoryAsync("History");
            Assert.Empty(emptyCategory);
        }

        [Fact]
        public async Task LoadFromFileAsync_ShouldAddQuestionsFromJsonFile()
        {
            var db = GetInMemoryDb();
            var service = CreateService(db);

            // Create a temporary JSON file with questions
            var questionsJson = @"
            [
                {
                    ""text"": ""Which empire was ruled by Genghis Khan?"",
                    ""answers"": [""Ottoman Empire"", ""Mongol Empire"", ""Roman Empire"", ""Persian Empire""],
                    ""correctAnswerIndexes"": [1],
                    ""difficulty"": ""easy"",
                    ""category"": ""History""
                },
                {
                    ""text"": ""Who was the leader of the Soviet Union during World War II?"",
                    ""answers"": [""Vladimir Lenin"", ""Joseph Stalin"", ""Nikita Khrushchev"", ""Leon Trotsky""],
                    ""correctAnswerIndexes"": [1],
                    ""difficulty"": ""easy"",
                    ""category"": ""History""
                }
            ]";

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, questionsJson);

            // Act
            var countAdded = await service.LoadFromFileAsync(tempFile);

            // Assert
            Assert.Equal(2, countAdded); // Two questions should be added
            var allQuestions = await service.GetAllAsync();
            Assert.Equal(2, allQuestions.Count);
            Assert.Contains(allQuestions, q => q.Text == "Which empire was ruled by Genghis Khan?");
            Assert.Contains(allQuestions, q => q.Text == "Who was the leader of the Soviet Union during World War II?");

            // Cleanup
            File.Delete(tempFile);
        }

    }
}
