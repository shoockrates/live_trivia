namespace live_trivia.Services;
using live_trivia.Repositories;
using live_trivia.Dtos;
using live_trivia.Interfaces;
using live_trivia.Extensions;
public class GameService : IGameService
{
    private readonly GamesRepository _gamesRepo;
    private readonly QuestionsRepository _questionsRepo;

    public GameService(GamesRepository gamesRepo, QuestionsRepository questionsRepo)
    {
        _gamesRepo = gamesRepo;
        _questionsRepo = questionsRepo;
    }
    public async Task<bool> StartGameAsync(string roomId)
    {
        var game = await _gamesRepo.GetGameAsync(roomId, includePlayers: true, includeQuestions: true);
        if (game == null)
            throw new Exceptions.GameNotFoundException(roomId);

        if (game.State == GameState.InProgress)
            return true;

        if (game.GamePlayers.Count < 1)
        {
            throw new Exceptions.GameStateException("At least one player is required to start the game.");
        }
        // Load game settings
        var settings = await _gamesRepo.GetGameSettingsAsync(roomId);
        if (settings == null || string.IsNullOrWhiteSpace(settings.Category) || settings.QuestionCount <= 0)
            return false;

        // Fetch random questions from QuestionsRepository
        var questions = await _questionsRepo.GetRandomQuestionsAsync(settings.QuestionCount, settings.Category, settings.Difficulty);

        if (questions.Count < settings.QuestionCount)
        {
            throw new Exceptions.NotEnoughQuestionsException(settings.Category!, settings.QuestionCount);
        }

        // Replace previous questions
        game.Questions.Clear();
        foreach (var q in questions)
            game.Questions.Add(q);

        game.State = GameState.InProgress;
        game.StartedAt = DateTime.UtcNow;
        game.CurrentQuestionIndex = 0;

        await _gamesRepo.SaveChangesAsync();
        return true;
    }

    public async Task<Game?> GetGameAsync(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
            throw new ArgumentException("RoomId cannot be null or empty.", nameof(roomId));

        var game = await _gamesRepo.GetGameAsync(roomId);
        return game;
    }

    public async Task<GameDetailsDto?> GetGameDetailsAsync(string roomId)
    {
        var game = await _gamesRepo.GetGameDetailsAsync(roomId);
        if (game == null)
        {
            throw new Exceptions.GameNotFoundException(roomId);
        }

        var questions = game.Questions.ToList();
        var currentQuestion = (game.CurrentQuestionIndex >= 0 && game.CurrentQuestionIndex < questions.Count)
            ? questions[game.CurrentQuestionIndex]
            : null;

        var currentAnswers = game.PlayerAnswers
            .Where(pa => pa.QuestionId == currentQuestion?.Id)
            .ToList();

        return new GameDetailsDto
        {
            CreatedAt = game.CreatedAt,
            StartedAt = game.StartedAt,
            HostPlayerId = game.HostPlayerId,
            RoomId = game.RoomId,
            State = game.State.ToString(),
            CurrentQuestionText = currentQuestion?.Text,
            CurrentQuestionAnswers = currentQuestion?.Answers,
            CurrentQuestionIndex = game.CurrentQuestionIndex,
            TotalQuestions = questions.Count,
            Players = game.GamePlayers.Select(gp => new GamePlayerDto
            {
                PlayerId = gp.PlayerId,
                Name = gp.Player.Name,
                CurrentScore = gp.Player.Score,
                HasSubmittedAnswer = currentAnswers.Any(pa => pa.PlayerId == gp.PlayerId)
            }).ToList()
        };
    }
    public async Task<GameSettings?> GetGameSettingsAsync(string roomId)
    {
        return await _gamesRepo.GetGameSettingsAsync(roomId);
    }

    public async Task<Game> CreateGameAsync(string roomId, Player hostPlayer)
    {
        var game = new Game
        {
            RoomId = roomId,
            HostPlayer = hostPlayer,
            State = GameState.WaitingForPlayers,
            CreatedAt = DateTime.UtcNow
        };
        await _gamesRepo.AddSync(game);
        await _gamesRepo.SaveChangesAsync();
        return game;
    }
    public async Task<GameSettings> UpdateGameSettingsAsync(string roomId, GameSettingsDto dto)
    {
        var game = await _gamesRepo.GetGameAsync(roomId);
        if (game?.State == GameState.InProgress)
            throw new Exceptions.GameStateException("Cannot change settings after game started.");

        var settings = await _gamesRepo.GetGameSettingsAsync(roomId);
        if (settings == null)
        {
            settings = new GameSettings { GameRoomId = roomId };
            await _gamesRepo.AddGameSettings(settings);
        }

        settings.Category = string.IsNullOrWhiteSpace(dto.Category)
            ? "Geography"
            : dto.Category.ToLower().CapitalizeFirstLetter();

        settings.Difficulty = string.IsNullOrWhiteSpace(dto.Difficulty)
            ? "medium"
            : dto.Difficulty.ToLower();
        settings.QuestionCount = dto.QuestionCount;
        settings.TimeLimitSeconds = dto.TimeLimitSeconds > 0 ? dto.TimeLimitSeconds : 15;
        await _gamesRepo.SaveChangesAsync();
        return settings;
    }

    public async Task<Player?> GetPlayerByIdAsync(int playerId)
    {
        return await _gamesRepo.GetPlayerByIdAsync(playerId);
    }

    public async Task AddExistingPlayerToGameAsync(Game game, Player player)
    {
        if (game == null)
        {
            throw new Exceptions.GameNotFoundException("Game must not be null.");
        }
        if (player == null)
        {
            throw new Exceptions.GameStateException("Player must not be null.");
        }

        var gamePlayer = new GamePlayer
        {
            GameRoomId = game.RoomId,
            PlayerId = player.Id,
        };
        await _gamesRepo.AddGamePlayerAsync(gamePlayer);
    }

    public async Task SaveChangesAsync()
    {
        await _gamesRepo.SaveChangesAsync();
    }
        
}
