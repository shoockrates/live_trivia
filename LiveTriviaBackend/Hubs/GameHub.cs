using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using live_trivia.Repositories;
using live_trivia.Interfaces;

namespace live_trivia.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        private readonly IGameService _gameService;
        private readonly GamesRepository _gamesRepository;
        private readonly IActiveGamesService _activeGamesService;


        private static readonly Dictionary<string, CategoryVotingState> _categoryVotes = new();

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


        public async Task StartCategoryVoting(string roomId, List<string> categories)
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

            // Only host can start voting
            if (game.HostPlayerId != playerId)
            {
                await Clients.Caller.SendAsync("Error", "Only the host can start voting");
                return;
            }

            if (categories == null || categories.Count == 0)
            {
                await Clients.Caller.SendAsync("Error", "No categories provided for voting");
                return;
            }

            var state = new CategoryVotingState
            {
                RoomId = roomId,
                Categories = categories.Distinct().ToList(),
                PlayerVotes = new Dictionary<int, string>()
            };

            _categoryVotes[roomId] = state;

            await Clients.Group(roomId).SendAsync("CategoryVotingStarted", new
            {
                RoomId = roomId,
                Categories = state.Categories
            });
        }


        public async Task SubmitCategoryVote(string roomId, string category)
        {
            var playerIdClaim = Context.User?.FindFirst("playerId");
            var playerName = Context.User?.Identity?.Name;

            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                await Clients.Caller.SendAsync("Error", "Player identity not found");
                return;
            }

            if (!_categoryVotes.TryGetValue(roomId, out var state))
            {
                await Clients.Caller.SendAsync("Error", "No active voting in this room");
                return;
            }

            if (!state.Categories.Contains(category))
            {
                await Clients.Caller.SendAsync("Error", "Invalid category");
                return;
            }

            // Save/overwrite player vote
            state.PlayerVotes[playerId] = category;

            // Build tallies
            var tallies = state.Categories
                .ToDictionary(
                    c => c,
                    c => state.PlayerVotes.Count(v => v.Value == c)
                );

            await Clients.Group(roomId).SendAsync("CategoryVoteUpdated", new
            {
                RoomId = roomId,
                PlayerId = playerId,
                PlayerName = playerName,
                SelectedCategory = category,
                Tallies = tallies
            });
        }


        public async Task EndCategoryVoting(string roomId)
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

            // Only host can end voting
            if (game.HostPlayerId != playerId)
            {
                await Clients.Caller.SendAsync("Error", "Only the host can end voting");
                return;
            }

            if (!_categoryVotes.TryGetValue(roomId, out var state) ||
                state.PlayerVotes.Count == 0)
            {
                await Clients.Caller.SendAsync("Error", "No active votes to finalize");
                return;
            }

            // Determine winner (simple max; tie-breaker = first)
            var winningCategory = state.PlayerVotes
                .GroupBy(v => v.Value)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .First()
                .Key;

            // Update GameSettings using existing service
            var settings = await _gameService.GetGameSettingsAsync(roomId);
            if (settings != null)
            {
                var dto = new Dtos.GameSettingsDto
                {
                    Category = winningCategory,
                    Difficulty = settings.Difficulty,
                    QuestionCount = settings.QuestionCount,
                    TimeLimitSeconds = settings.TimeLimitSeconds
                };

                await _gameService.UpdateGameSettingsAsync(roomId, dto);
            }

            _categoryVotes.Remove(roomId);

            await Clients.Group(roomId).SendAsync("CategoryVotingFinished", new
            {
                RoomId = roomId,
                WinningCategory = winningCategory
            });
        }
    }
}
