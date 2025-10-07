using live_trivia.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // React default port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer(); // registers endpoints for Swagger
builder.Services.AddSwaggerGen(); // generates Swagger JSON + UI
builder.Services.AddDbContext<TriviaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

app.UseCors("AllowReactApp");

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
