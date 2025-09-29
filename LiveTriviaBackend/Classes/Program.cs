using live_trivia;
using live_trivia.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddDbContext<TriviaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/", () => "LiveTriviaBackend is running!");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();

    // Ensure database tables are created
    await context.Database.EnsureCreatedAsync();

    // Only load questions if table is empty
    if (!context.Questions.Any())
    {
        try
        {
            var questionBank = new QuestionBank("questions.json");
            context.Questions.AddRange(questionBank.Questions);
            await context.SaveChangesAsync();
            Console.WriteLine("Loaded questions into database!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading questions: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("Questions already loaded.");
    }
}
app.MapGet("/questions/test", (TriviaDbContext context) =>
{
    var questionCount = context.Questions.Count();
    var sampleQuestion = context.Questions.First();

    return new
    {
        TotalQuestions = questionCount,
        SampleQuestion = new
        {
            sampleQuestion.Text,
            sampleQuestion.Answers,
            sampleQuestion.CorrectAnswerIndexes,
            sampleQuestion.Difficulty,
            sampleQuestion.Category
        }
    };
});
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
