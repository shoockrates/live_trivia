using live_trivia.Repositories;
using live_trivia.Services;
using live_trivia.Data;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

builder.Services.AddDbContext<TriviaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Register repository (one instance per HTTP request)
builder.Services.AddScoped<GamesRepository>();
builder.Services.AddScoped<QuestionsRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // User Secrets automatically loaded into Configuration
    var jwtKey = builder.Configuration["Jwt:Key"]
                 ?? throw new InvalidOperationException("JWT Key not configured. Run: dotnet user-secrets set \"Jwt:Key\" \"your-secret\"");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "live-trivia",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "live-trivia-users",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

// Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// Map API controllers
app.MapControllers();

// Run the app
app.Run();
