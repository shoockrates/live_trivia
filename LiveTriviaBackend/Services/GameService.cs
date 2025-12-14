namespace live_trivia.Services;
using live_trivia.Repositories;
using live_trivia.Dtos;
using live_trivia.Interfaces;
using live_trivia.Exceptions;

public class GameService : IGameService
{
    private readonly GamesRepository _gamesRepo;
    private readonly QuestionsRepository _questionsRepo;
    private readonly IActiveGamesService _activeGamesService;

    public GameService(GamesRepository gamesRepo, QuestionsRepository questionsRepo, IActiveGamesService activeGamesService)
    {
        _gamesRepo = gamesRepo;
        _questionsRepo = questionsRepo;
        _activeGamesService = activeGamesService;

    }

    public async Task<bool> StartGameAsync(string roomId)
    {
        var game = await _gamesRepo.GetGameAsync(roomId, includePlayers: true, includeQuestions: true);
        if (game == null)
            throw new Exceptions.GameNotFoundException(roomId);

        if (game.State == GameState.InProgress)
        {
            return true;
        }

        if (game.GamePlayers.Count < 1)
        {
            throw new Exceptions.GameStateException("At least one player is required to start the game.");
        }

        foreach (var gp in game.GamePlayers)
        {
            if (gp.Player != null)
            {
                gp.Player.Score = 0;
            }
        }

        // Load game settings
        var settings = await _gamesRepo.GetGameSettingsAsync(roomId);
        if (settings == null || string.IsNullOrWhiteSpace(settings.Category) || settings.QuestionCount <= 0)
            return false;

        var questions = await _questionsRepo.GetRandomQuestionsAsync(
            settings.QuestionCount,
            settings.Category,
            settings.Difficulty
        );

        if (questions.Count < settings.QuestionCount)
        {
            throw new Exceptions.NotEnoughQuestionsException(settings.Category!, settings.QuestionCount);
        }

        game.Questions.Clear();
        foreach (var q in questions)
            game.Questions.Add(q);

        game.State = GameState.InProgress;
        game.StartedAt = DateTime.UtcNow;
        game.CurrentQuestionIndex = 0;

        // Make sure the reset + setup is saved
        await _gamesRepo.SaveChangesAsync();

        return true;
    }


    public async Task<Game?> GetGameAsync(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
            throw new ArgumentException("RoomId cannot be null or empty.", nameof(roomId));

        var game = await _gamesRepo.GetGameAsync(
            roomId,
            includePlayers: true,
            includeQuestions: true,
            includeAnswers: true
        );

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
            CategoryVotes = game.CategoryVotes,
            PlayerVotes = game.PlayerVotes,
            Players = game.GamePlayers.Select(gp => new GamePlayerDto
            {
                PlayerId = gp.PlayerId,
                Name = gp.Player.Name,
                CurrentScore = gp.Player.Score,
                Score = gp.Player.Score,
                HasSubmittedAnswer = currentAnswers.Any(pa => pa.PlayerId == gp.PlayerId)
            }).ToList(),
            Questions = questions.Select(q => new QuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Answers = q.Answers,
                CorrectAnswerIndexes = q.CorrectAnswerIndexes,
                Category = q.Category,
                Difficulty = q.Difficulty
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
            HostPlayerId = hostPlayer.Id,
            HostPlayer = hostPlayer,
            State = GameState.WaitingForPlayers,
            CreatedAt = DateTime.UtcNow
        };

        await _gamesRepo.AddSync(game);
        await _gamesRepo.SaveChangesAsync();

        _activeGamesService.TryAddGame(roomId);

        // Create default game settings
        var settings = new GameSettings
        {
            GameRoomId = roomId,
            Category = "any",
            Difficulty = "medium",
            QuestionCount = 10,
            TimeLimitSeconds = 30
        };

        await _gamesRepo.AddGameSettings(settings);

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

        // Normalize category name before saving
        string normalizedCategory = string.IsNullOrWhiteSpace(dto.Category)
            ? "Geography"
            : NormalizeCategoryName(dto.Category);

        settings.Category = normalizedCategory;

        settings.Difficulty = string.IsNullOrWhiteSpace(dto.Difficulty)
            ? "medium"
            : dto.Difficulty.ToLower();

        settings.QuestionCount = dto.QuestionCount;
        settings.TimeLimitSeconds = dto.TimeLimitSeconds > 0 ? dto.TimeLimitSeconds : 15;
        await _gamesRepo.SaveChangesAsync();
        return settings;
    }

    // Helper method to normalize category names (consistent with QuestionsRepository)
    private string NormalizeCategoryName(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return category;

        // Convert to lowercase and trim
        category = category.Trim().ToLower();

        // Capitalize first letter of each word for general cases
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(category);
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

    public async Task CleanupGameAsync(string roomId)
    {
        try
        {
            Console.WriteLine($"Starting cleanup for game room: {roomId}");

            // First check if game exists
            var game = await _gamesRepo.GetGameAsync(roomId);
            if (game == null)
            {
                Console.WriteLine($"Game {roomId} not found in database");
                _activeGamesService.TryRemoveGame(roomId);
                return;
            }

            Console.WriteLine($"Found game {roomId}, proceeding with deletion");

            // Delete the game from database (cascades to related records)
            await _gamesRepo.DeleteGameAsync(roomId);

            // Remove from active games tracking
            _activeGamesService.TryRemoveGame(roomId);

            Console.WriteLine($"Successfully cleaned up game room: {roomId}");
        }
        catch (Exception ex)
        {

            Console.WriteLine($"Error cleaning up game {roomId}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    public async Task RecordCategoryVoteAsync(string roomId, int playerId, string category)
    {
        var game = await _gamesRepo.GetGameAsync(roomId, includePlayers: true);

        if (game == null)
        {
            throw new GameNotFoundException(roomId);
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new GeneralServiceException("Category cannot be empty.");
        }

        if (game.State != GameState.WaitingForPlayers)
        {
            throw new GameStateException("Votes are only accepted while waiting for players.");
        }

        if (!game.GamePlayers.Any(gp => gp.PlayerId == playerId))
        {
            throw new PlayerNotInGameException(playerId, roomId);
        }

        string normalizedCategory = NormalizeCategoryName(category);


        if (game.PlayerVotes.TryGetValue(playerId, out string? oldCategory))
        {

            if (oldCategory.Equals(normalizedCategory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (game.CategoryVotes.ContainsKey(oldCategory))
            {
                game.CategoryVotes[oldCategory]--;

                if (game.CategoryVotes[oldCategory] <= 0)
                {
                    game.CategoryVotes.Remove(oldCategory);
                }
            }
        }

        game.PlayerVotes[playerId] = normalizedCategory;

        if (game.CategoryVotes.ContainsKey(normalizedCategory))
        {
            game.CategoryVotes[normalizedCategory]++;
        }
        else
        {
            game.CategoryVotes.Add(normalizedCategory, 1);
        }

        await _gamesRepo.SaveChangesAsync();
    }


    public async Task<GameDetailsDto> ResetGameAsync(string roomId, int requestingPlayerId)
    {
        var game = await _gamesRepo.GetGameAsync(roomId, includePlayers: true, includeQuestions: true, includeAnswers: true);
        if (game == null) throw new GameNotFoundException(roomId);

        if (game.HostPlayerId != requestingPlayerId)
            throw new GameStateException("Only the host can reset the game.");

        Console.WriteLine($"Resetting game {roomId} - Current state: {game.State}");

        // Reset state - allow reset from any state (Finished, InProgress, etc)
        game.State = GameState.WaitingForPlayers;
        game.StartedAt = null;
        game.EndedAt = null;
        game.CurrentQuestionIndex = -1;

        // Reset scores
        foreach (var gp in game.GamePlayers)
        {
            if (gp.Player != null)
            {
                gp.Player.Score = 0;
            }
        }

        // Clear answers + questions + voting
        await _gamesRepo.RemoveAnswersForGameAsync(roomId);
        game.Questions.Clear();
        game.ClearVoting();

        await _gamesRepo.SaveChangesAsync();

        Console.WriteLine($"Game {roomId} reset successfully - New state: {game.State}");

        return (await GetGameDetailsAsync(roomId))!;
    }


}
