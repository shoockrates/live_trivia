using live_trivia;
using live_trivia.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer(); // registers endpoints for Swagger
builder.Services.AddSwaggerGen(); // generates Swagger JSON + UI
builder.Services.AddDbContext<TriviaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();       // serve swagger.json
    app.UseSwaggerUI(c =>   // serve UI at /swagger
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LiveTrivia API V1");
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapGet("/", () => "Backend working!");

app.Run();
