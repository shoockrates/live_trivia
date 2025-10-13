using live_trivia.Data;
using live_trivia.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Allow requests from React frontend (localhost:3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add controllers and JSON settings (prevent cycles, pretty output)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Register repository (one instance per HTTP request)
builder.Services.AddScoped<GamesRepository>();
builder.Services.AddScoped<QuestionsRepository>();

// Configure Entity Framework + PostgreSQL
builder.Services.AddDbContext<TriviaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Enable Swagger (API docs and test UI)
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable CORS for frontend
app.UseCors("AllowReactApp");

// Enable Swagger only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LiveTrivia API V1");
    });
}

// Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// Map API controllers and root test route
app.MapControllers();
app.MapGet("/", () => "Backend working!");

// Run the app
app.Run();
