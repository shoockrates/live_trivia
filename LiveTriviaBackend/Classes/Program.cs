using live_trivia;
using live_trivia.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Simple CORS for frontend requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Register QuestionBank to read from questions.json
builder.Services.AddSingleton(new QuestionBank("questions.json"));


builder.Services.AddDbContext<TriviaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.MapGet("/", () => "LiveTriviaBackend is running!");

try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();

        // Ensure DB exists and seed from JSON on first run
        context.Database.EnsureCreated();

        if (!context.Questions.Any())
        {
            try
            {
                var questionBank = new QuestionBank("questions.json");
                context.Questions.AddRange(questionBank.Questions);
                context.SaveChanges();
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
}
catch (Exception ex)
{
    Console.WriteLine($"Database unavailable. Continuing without DB. Details: {ex.Message}");
}

// Simple endpoint to get questions from JSON by optional category
app.MapGet("/questions", (string? category, QuestionBank questionBank) =>
{
    var questions = string.IsNullOrWhiteSpace(category)
        ? questionBank.Questions
        : questionBank.Questions.Where(q => string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
    return Results.Ok(questions);
});

// Create a new game room
app.MapPost("/games/{roomId}", async (string roomId, TriviaDbContext context) =>
{
    var game = new Game(roomId);
    context.Games.Add(game);
    await context.SaveChangesAsync();
    return Results.Created($"/games/{roomId}", game);
});


// Join a game
app.MapPost("/games/{roomId}/players", async (string roomId, string playerName, TriviaDbContext context) =>
{
    var game = await context.Games.FindAsync(roomId);
    if (game == null) return Results.NotFound("Game not found");
    var player = new Player { Name = playerName };
    game.addPlayer(player);
    await context.SaveChangesAsync();
    return Results.Ok(player);
});

// Get random question
app.MapGet("/questions/random", async (TriviaDbContext context) =>
{
    var count = await context.Questions.CountAsync();
    var random = new Random().Next(count);
    var question = await context.Questions.Skip(random).FirstAsync();
    return Results.Ok(question);
});

// Get questions by category
app.MapGet("/questions/category/{category}", async (string category, TriviaDbContext context) =>
{
    var questions = await context.Questions
        .Where(q => q.Category == category)
        .ToListAsync();
    return Results.Ok(questions);
});

app.Run();

