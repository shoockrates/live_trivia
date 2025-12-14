using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using live_trivia.Repositories;
using live_trivia.Interfaces;
using System.Collections.Concurrent;
using live_trivia.Dtos;
using System.Diagnostics.CodeAnalysis;

namespace live_trivia.Hubs
{
    [ExcludeFromCodeCoverage]
    [Authorize]
    public class GameHub : Hub
    {
        private readonly IGameService _gameService;
        private readonly GamesRepository _gamesRepository;
        private readonly IActiveGamesService _activeGamesService;

        private static readonly ConcurrentDictionary<string, CategoryVotingState> _categoryVotingStates
            = new();

        public GameHub(IGameService gameService, GamesRepository gamesRepository, IActiveGamesService activeGamesService)
        {
            _gameService = gameService;
            _gamesRepository = gamesRepository;
            _activeGamesService = activeGamesService;
        }

        public async Task JoinGameRoom(string roomId)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                // Get player ID from claims
                var playerIdClaim = Context.User?.FindFirst("playerId");
                var playerName = Context.User?.Identity?.Name;

                if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
                {
                    await Clients.Caller.SendAsync("Error", "Player identity not found");
                    return;
                }

                Console.WriteLine($"Player {playerName} (ID: {playerId}) joined room {roomId}");

                // Get updated game state
                var gameDetails = await _gameService.GetGameDetailsAsync(roomId);
                if (gameDetails != null)
                {
                    // Notify ALL players in the room about the updated game state
                    await Clients.Group(roomId).SendAsync("PlayerJoined", new
                    {
                        Player = new { Id = playerId, Name = playerName },
                        GameState = gameDetails,
                        ConnectionId = Context.ConnectionId,
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in JoinGameRoom: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "Failed to join game room");
            }
        }

        public async Task LeaveGameRoom(string roomId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

                var playerName = Context.User?.Identity?.Name;
                var playerIdClaim = Context.User?.FindFirst("playerId");

                if (playerIdClaim != null && int.TryParse(playerIdClaim.Value, out int playerId))
                {
                    Console.WriteLine($"Player {playerName} (ID: {playerId}) left room {roomId}");

                    // Get updated game state after player left
                    var gameDetails = await _gameService.GetGameDetailsAsync(roomId);

                    await Clients.Group(roomId).SendAsync("PlayerLeft", new
                    {
                        PlayerId = playerId,
                        PlayerName = playerName,
                        ConnectionId = Context.ConnectionId,
                        Timestamp = DateTime.UtcNow,
                        GameState = gameDetails // Include updated game state
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LeaveGameRoom: {ex.Message}");
            }
        }

        public async Task StartGame(string roomId)
        {
            try
            {
                var playerIdClaim = Context.User?.FindFirst("playerId");
                if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
                {
                    await Clients.Caller.SendAsync("GameStartFailed", "Player identity not found");
                    return;
                }

                var game = await _gameService.GetGameAsync(roomId);
                if (game == null)
                {
                    await Clients.Caller.SendAsync("GameStartFailed", "Game not found");
                    return;
                }

                // Check if player is host
                if (game.HostPlayerId != playerId)
                {
                    await Clients.Caller.SendAsync("GameStartFailed", "Only the host can start the game");
                    return;
                }

                // Check if there are enough players
                if (game.GamePlayers.Count < 1)
                {
                    await Clients.Caller.SendAsync("GameStartFailed", "Need at least 1 player to start");
                    return;
                }

                var success = await _gameService.StartGameAsync(roomId);
                if (success)
                {
                    var gameDetails = await _gameService.GetGameDetailsAsync(roomId);
                    await Clients.Group(roomId).SendAsync("GameStarted", gameDetails);
                }
                else
                {
                    await Clients.Caller.SendAsync("GameStartFailed", "Failed to start game. Ensure there are players and questions.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StartGame: {ex.Message}");
                await Clients.Caller.SendAsync("GameStartFailed", $"Error starting game: {ex.Message}");
            }
        }

        public async Task SubmitAnswer(string roomId, int questionId, List<int> selectedAnswers)
        {
            try
            {
                var playerIdClaim = Context.User?.FindFirst("playerId");
                var playerName = Context.User?.Identity?.Name;

                if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
                {
                    Console.WriteLine("Player identity not found in SubmitAnswer");
                    return;
                }

                Console.WriteLine($"Player {playerName} (ID: {playerId}) submitted answer for question {questionId}");

                // Store the answer and notify others
                var game = await _gameService.GetGameAsync(roomId);
                if (game != null)
                {
                    await Clients.Group(roomId).SendAsync("AnswerSubmitted", new
                    {
                        PlayerId = playerId,
                        PlayerName = playerName,
                        ConnectionId = Context.ConnectionId,
                        QuestionId = questionId,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SubmitAnswer: {ex.Message}");
            }
        }

        public async Task NextQuestion(string roomId)
        {
            try
            {
                var playerIdClaim = Context.User?.FindFirst("playerId");
                if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
                {
                    await Clients.Caller.SendAsync("Error", "Player identity not found");
                    return;
                }

                var game = await _gameService.GetGameAsync(roomId);
                if (game == null)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                // Only host can advance
                if (game.HostPlayerId != playerId)
                {
                    await Clients.Caller.SendAsync("Error", "Only the host can advance the game");
                    return;
                }

                // Score current question before moving
                game.ScoreCurrentQuestion();
                var moved = game.MoveNextQuestion();
                await _gameService.SaveChangesAsync();

                if (moved)
                {
                    // Get full game details with questions
                    var gameDetails = await _gameService.GetGameDetailsAsync(roomId);

                    // Broadcast to all players in the room
                    await Clients.Group(roomId).SendAsync("NextQuestion", gameDetails);

                    Console.WriteLine($"Broadcasted NextQuestion for room {roomId}, question index: {gameDetails.CurrentQuestionIndex}");
                }
                else
                {
                    // Game finished
                    var leaderboard = game.GetLeaderboard();
                    await Clients.Group(roomId).SendAsync("GameFinished", new
                    {
                        Leaderboard = leaderboard,
                        FinalScores = leaderboard.Select(p => new { p.Name, p.Score })
                    });

                    // Cleanup game after a short delay, in the same scope
                    await Task.Delay(5000);
                    await _gameService.CleanupGameAsync(roomId);
                    Console.WriteLine($"Cleaned up game room: {roomId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in NextQuestion: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error advancing to next question: {ex.Message}");
            }
        }

        public override async Task OnConnectedAsync()
        {
            var playerName = Context.User?.Identity?.Name;
            var playerIdClaim = Context.User?.FindFirst("playerId");

            Console.WriteLine($"SignalR Connection Established: {Context.ConnectionId}");
            Console.WriteLine($"Player: {playerName}, ID: {playerIdClaim?.Value}");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var playerName = Context.User?.Identity?.Name;
            Console.WriteLine($"SignalR Connection Lost: {Context.ConnectionId}, Player: {playerName}");

            if (exception != null)
            {
                Console.WriteLine($"Disconnect reason: {exception.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // CATEGORY VOTING METHODS

        public async Task StartCategoryVoting(string roomId, List<string> categories)
        {
            var playerId = await GetCurrentPlayerId();
            var game = await _gameService.GetGameAsync(roomId);
            if (game == null) return;

            if (game.HostPlayerId != playerId)
            {
                await Clients.Caller.SendAsync("Error", "Only host can start voting");
                return;
            }

            var state = new CategoryVotingState
            {
                RoomId = roomId,
                Categories = categories.Distinct().ToList(),
                PlayerVotes = new Dictionary<int, string>(),
                Round = 1,
                StartedAt = DateTime.UtcNow,
                DurationSeconds = 60
            };

            _categoryVotingStates[roomId] = state;

            await Clients.Group(roomId).SendAsync("CategoryVotingStarted", new
            {
                Categories = state.Categories,
                Round = state.Round,
                DurationSeconds = state.DurationSeconds
            });

            _ = Task.Run(async () =>
            {
                var endAt = state.StartedAt.AddSeconds(state.DurationSeconds);
                while (DateTime.UtcNow < endAt)
                {
                    var remaining = (int)(endAt - DateTime.UtcNow).TotalSeconds;
                    if (remaining < 0) remaining = 0;

                    await Clients.Group(roomId).SendAsync("CategoryVotingTimer", new
                    {
                        RemainingSeconds = remaining
                    });

                    if (remaining == 0) break;
                    await Task.Delay(1000);
                }

                // Auto-end if still active
                if (_categoryVotingStates.ContainsKey(roomId))
                {
                    await EndCategoryVoting(roomId);
                }
            });
        }

        public async Task SubmitCategoryVote(string roomId, string category)
        {
            var playerId = await GetCurrentPlayerId();

            if (!_categoryVotingStates.TryGetValue(roomId, out var state))
                return;

            if (!state.Categories.Contains(category))
                return;

            state.PlayerVotes[playerId] = category;

            var tallies = state.Categories
                .ToDictionary(c => c, c => state.PlayerVotes.Values.Count(v => v == c));

            await Clients.Group(roomId).SendAsync("CategoryVoteUpdated", new
            {
                Tallies = tallies,
                PlayerId = playerId,
                SelectedCategory = category,
                Round = state.Round
            });
        }

        public async Task EndCategoryVoting(string roomId)
        {
            var playerId = await GetCurrentPlayerId();
            var game = await _gameService.GetGameAsync(roomId);
            if (game == null) return;

            if (game.HostPlayerId != playerId)
            {
                await Clients.Caller.SendAsync("Error", "Only the host can end voting");
                return;
            }

            if (!_categoryVotingStates.TryGetValue(roomId, out var state))
                return;

            var tallies = state.Categories
                .ToDictionary(c => c, c => state.PlayerVotes.Values.Count(v => v == c));

            var maxVotes = tallies.Values.DefaultIfEmpty(0).Max();
            var topCategories = tallies
                .Where(kv => kv.Value == maxVotes && maxVotes > 0)
                .Select(kv => kv.Key)
                .ToList();

            if (topCategories.Count == 0)
            {
                // No votes at all – just let host pick freely
                await Clients.Group(roomId).SendAsync("CategoryVotingFinished", new
                {
                    WinningCategory = (string?)null,
                    Round = state.Round,
                    IsTie = false,
                    IsFinal = false,
                    TiedCategories = new List<string>()
                });

                _categoryVotingStates.TryRemove(roomId, out _);
                return;
            }

            // CASE 1: clear winner
            if (topCategories.Count == 1)
            {
                var winner = topCategories[0];

                // Get current settings to preserve other fields
                var currentSettings = await _gameService.GetGameSettingsAsync(roomId);

                var gameSettingsDto = new GameSettingsDto
                {
                    Category = winner,
                    Difficulty = currentSettings?.Difficulty ?? "any",
                    QuestionCount = currentSettings?.QuestionCount ?? 5,
                    TimeLimitSeconds = currentSettings?.TimeLimitSeconds ?? 30
                };

                await _gameService.UpdateGameSettingsAsync(roomId, gameSettingsDto);

                await Clients.Group(roomId).SendAsync("CategoryVotingFinished", new
                {
                    WinningCategory = winner,
                    Round = state.Round,
                    IsTie = false,
                    IsFinal = true,
                    TiedCategories = new List<string>()
                });

                _categoryVotingStates.TryRemove(roomId, out _);
                return;
            }

            // CASE 2: tie
            if (state.Round == 1)
            {
                // First tie -> revote only among tied categories
                state.Round = 2;
                state.Categories = topCategories;
                state.PlayerVotes.Clear();
                state.StartedAt = DateTime.UtcNow;

                await Clients.Group(roomId).SendAsync("CategoryRevoteStarted", new
                {
                    Categories = state.Categories,
                    Round = state.Round,
                    TiedCategories = topCategories,
                    DurationSeconds = state.DurationSeconds
                });

                _ = Task.Run(async () =>
                {
                    var endAt = state.StartedAt.AddSeconds(state.DurationSeconds);
                    while (DateTime.UtcNow < endAt)
                    {
                        var remaining = (int)(endAt - DateTime.UtcNow).TotalSeconds;
                        if (remaining < 0) remaining = 0;

                        await Clients.Group(roomId).SendAsync("CategoryVotingTimer", new
                        {
                            RemainingSeconds = remaining
                        });

                        if (remaining == 0) break;
                        await Task.Delay(1000);
                    }

                    if (_categoryVotingStates.ContainsKey(roomId))
                    {
                        await EndCategoryVoting(roomId);
                    }
                });

                return;
            }

            // CASE 3: tie again in round 2 -> host decides
            var hostVote = state.PlayerVotes.TryGetValue(game.HostPlayerId!.Value, out var hostChoice)
                && topCategories.Contains(hostChoice)
                ? hostChoice
                : topCategories.First();

            // Get current settings to preserve other fields
            var existingGameSettings = await _gameService.GetGameSettingsAsync(roomId);

            var updatedSettings = new GameSettingsDto
            {
                Category = hostVote,
                Difficulty = existingGameSettings?.Difficulty ?? "any",
                QuestionCount = existingGameSettings?.QuestionCount ?? 5,
                TimeLimitSeconds = existingGameSettings?.TimeLimitSeconds ?? 30
            };

            await _gameService.UpdateGameSettingsAsync(roomId, updatedSettings);

            await Clients.Group(roomId).SendAsync("CategoryVotingFinished", new
            {
                WinningCategory = hostVote,
                Round = state.Round,
                IsTie = true,
                IsFinal = true,
                TiedCategories = topCategories
            });

            _categoryVotingStates.TryRemove(roomId, out _);
        }

        private Task<int> GetCurrentPlayerId()
        {
            var playerIdClaim = Context.User?.FindFirst("playerId");

            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                // HubException will go back to the caller as an error
                throw new HubException("Player identity not found");
            }

            return Task.FromResult(playerId);
        }
    }
}
