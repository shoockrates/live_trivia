using Xunit;
using live_trivia;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace live_trivia.Tests.ModelTests
{
    public class QuestionBankTests
    {
        [Fact]
        public void Constructor_CreatesEmptyQuestionBank()
        {
            var bank = new QuestionBank();

            Assert.Empty(bank.Questions);
            Assert.Equal(0, bank.Count);
        }

        [Fact]
        public void Constructor_ThrowsException_WhenFileNotFound()
        {
            Assert.Throws<FileNotFoundException>(() =>
                new QuestionBank("nonexistent.json")
            );
        }

        [Fact]
        public void Constructor_LoadsQuestions_FromJsonFile()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var questions = new List<Question>
                {
                    new Question("Test Question 1", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography"),
                    new Question("Test Question 2", new List<string> { "C", "D" }, new List<int> { 1 }, "Medium", "History")
                };

                var json = JsonSerializer.Serialize(questions, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tempFile, json);

                var bank = new QuestionBank(tempFile);

                Assert.Equal(2, bank.Count);
                Assert.Equal(2, bank.Questions.Count);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetRandomQuestion_ThrowsException_WhenNoQuestions()
        {
            var bank = new QuestionBank();

            Assert.Throws<InvalidOperationException>(() =>
                bank.GetRandomQuestion()
            );
        }

        [Fact]
        public void GetRandomQuestion_ReturnsQuestion_WhenQuestionsExist()
        {
            var bank = new QuestionBank();
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            bank.Questions.Add(question);

            var result = bank.GetRandomQuestion();

            Assert.NotNull(result);
            Assert.Equal("Test Question", result.Text);
        }

        [Fact]
        public void GetQuestionByCategory_ThrowsException_WhenNoQuestionsForCategory()
        {
            var bank = new QuestionBank();
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            bank.Questions.Add(question);

            Assert.Throws<InvalidOperationException>(() =>
                bank.GetQuestionByCategory("History")
            );
        }

        [Fact]
        public void GetQuestionByCategory_ReturnsQuestion_WhenCategoryExists()
        {
            var bank = new QuestionBank();
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            bank.Questions.Add(question);

            var result = bank.GetQuestionByCategory("Geography");

            Assert.NotNull(result);
            Assert.Equal("Geography", result.Category);
        }

        [Fact]
        public void GetQuestionByCategory_IsCaseInsensitive()
        {
            var bank = new QuestionBank();
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            bank.Questions.Add(question);

            var result = bank.GetQuestionByCategory("geography");

            Assert.NotNull(result);
            Assert.Equal("Geography", result.Category);
        }

        [Fact]
        public void GetQuestionByDifficulty_ThrowsException_WhenNoQuestionsForDifficulty()
        {
            var bank = new QuestionBank();
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            bank.Questions.Add(question);

            Assert.Throws<InvalidOperationException>(() =>
                bank.GetQuestionByDifficulty("Hard")
            );
        }

        [Fact]
        public void GetQuestionByDifficulty_ReturnsQuestion_WhenDifficultyExists()
        {
            var bank = new QuestionBank();
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            bank.Questions.Add(question);

            var result = bank.GetQuestionByDifficulty("Easy");

            Assert.NotNull(result);
            Assert.Equal("Easy", result.Difficulty);
        }

        [Fact]
        public void GetQuestionByDifficulty_IsCaseInsensitive()
        {
            var bank = new QuestionBank();
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            bank.Questions.Add(question);

            var result = bank.GetQuestionByDifficulty("easy");

            Assert.NotNull(result);
            Assert.Equal("Easy", result.Difficulty);
        }

        [Fact]
        public void GetQuestion_ThrowsException_WhenNoQuestionsForCategoryAndDifficulty()
        {
            var bank = new QuestionBank();
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            bank.Questions.Add(question);

            Assert.Throws<InvalidOperationException>(() =>
                bank.GetQuestion("History", "Easy")
            );
        }

        [Fact]
        public void GetQuestion_ReturnsQuestion_WhenCategoryAndDifficultyMatch()
        {
            var bank = new QuestionBank();
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            bank.Questions.Add(question);

            var result = bank.GetQuestion("Geography", "Easy");

            Assert.NotNull(result);
            Assert.Equal("Geography", result.Category);
            Assert.Equal("Easy", result.Difficulty);
        }

        [Fact]
        public void GetAllCategories_ReturnsDistinctCategories()
        {
            var bank = new QuestionBank();
            bank.Questions.Add(new Question("Q1", new List<string> { "A" }, new List<int> { 0 }, "Easy", "Geography"));
            bank.Questions.Add(new Question("Q2", new List<string> { "A" }, new List<int> { 0 }, "Easy", "History"));
            bank.Questions.Add(new Question("Q3", new List<string> { "A" }, new List<int> { 0 }, "Easy", "Geography"));

            var categories = bank.GetAllCategories();

            Assert.Equal(2, categories.Count);
            Assert.Contains("Geography", categories);
            Assert.Contains("History", categories);
        }

        [Fact]
        public void GetAllCategories_IsCaseInsensitive()
        {
            var bank = new QuestionBank();
            bank.Questions.Add(new Question("Q1", new List<string> { "A" }, new List<int> { 0 }, "Easy", "Geography"));
            bank.Questions.Add(new Question("Q2", new List<string> { "A" }, new List<int> { 0 }, "Easy", "geography"));

            var categories = bank.GetAllCategories();

            Assert.Single(categories);
        }

        [Fact]
        public void GetAllDifficulties_ReturnsDistinctDifficulties()
        {
            var bank = new QuestionBank();
            bank.Questions.Add(new Question("Q1", new List<string> { "A" }, new List<int> { 0 }, "Easy", "Geography"));
            bank.Questions.Add(new Question("Q2", new List<string> { "A" }, new List<int> { 0 }, "Medium", "History"));
            bank.Questions.Add(new Question("Q3", new List<string> { "A" }, new List<int> { 0 }, "Easy", "Geography"));

            var difficulties = bank.GetAllDifficulties();

            Assert.Equal(2, difficulties.Count);
            Assert.Contains("Easy", difficulties);
            Assert.Contains("Medium", difficulties);
        }

        [Fact]
        public void RemoveQuestion_RemovesQuestion_FromList()
        {
            var bank = new QuestionBank();
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            bank.Questions.Add(question);

            bank.RemoveQuestion(question);

            Assert.Empty(bank.Questions);
            Assert.Equal(0, bank.Count);
        }
    }
}
