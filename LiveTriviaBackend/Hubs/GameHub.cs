using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using live_trivia.Repositories;
using live_trivia.Data;

namespace live_trivia.Hubs
{
    [Authorize] // Require authentication for all hub methods
    public class GameHub : Hub
    {
        private readonly GamesRepository _gamesRepository;
        private readonly TriviaDbContext _context;

        public GameHub(GamesRepository gamesRepository, TriviaDbContext context)
        {
            _gamesRepository = gamesRepository;
            _context = context;
        }

        public async Task JoinGameRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // Get player ID from claims
            var playerIdClaim = Context.User?.FindFirst("playerId");
            var playerName = Context.User?.Identity?.Name;

            Console.WriteLine($"Player {playerName} (ID: {playerIdClaim?.Value}) joined room {roomId}");

            // Notify other players that someone joined
            var game = await _gamesRepository.GetGameAsync(roomId);
            if (game != null)
            {
                var gameDetails = await _gamesRepository.GetGameDetailsAsync(roomId);
                await Clients.Group(roomId).SendAsync("PlayerJoined", new
                {
                    PlayerId = Context.ConnectionId,
                    PlayerName = playerName,
                    Timestamp = DateTime.UtcNow,
                    GameState = gameDetails
                });
            }
        }

        public async Task LeaveGameRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

            var playerName = Context.User?.Identity?.Name;
            Console.WriteLine($"Player {playerName} left room {roomId}");

            await Clients.Group(roomId).SendAsync("PlayerLeft", new
            {
                PlayerId = Context.ConnectionId,
                PlayerName = playerName,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task StartGame(string roomId)
        {
            var playerIdClaim = Context.User?.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                await Clients.Caller.SendAsync("GameStartFailed", "Player identity not found");
                return;
            }

            var game = await _gamesRepository.GetGameAsync(roomId);
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

            var success = await _gamesRepository.StartGameAsync(roomId);
            if (success)
            {
                var gameDetails = await _gamesRepository.GetGameDetailsAsync(roomId);
                await Clients.Group(roomId).SendAsync("GameStarted", gameDetails);
            }
            else
            {
                await Clients.Caller.SendAsync("GameStartFailed", "Failed to start game. Ensure there are players and questions.");
            }
        }

        public async Task SubmitAnswer(string roomId, int questionId, List<int> selectedAnswers)
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
            var game = await _gamesRepository.GetGameAsync(roomId);
            if (game != null)
            {
                await Clients.Group(roomId).SendAsync("AnswerSubmitted", new
                {
                    PlayerId = Context.ConnectionId,
                    PlayerName = playerName,
                    QuestionId = questionId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public async Task NextQuestion(string roomId)
        {
            var playerIdClaim = Context.User?.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                return;
            }

            var game = await _gamesRepository.GetGameAsync(roomId);
            if (game == null)
            {
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
            await _gamesRepository.SaveChangesAsync();

            if (moved)
            {
                var gameDetails = await _gamesRepository.GetGameDetailsAsync(roomId);
                await Clients.Group(roomId).SendAsync("NextQuestion", gameDetails);
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
            }
        }

        public async Task UpdateGameSettings(string roomId, object settings)
        {
            var playerIdClaim = Context.User?.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                return;
            }

            var game = await _gamesRepository.GetGameAsync(roomId);
            if (game == null || game.HostPlayerId != playerId)
            {
                await Clients.Caller.SendAsync("Error", "Only the host can update settings");
                return;
            }

            await Clients.Group(roomId).SendAsync("SettingsUpdated", settings);
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
    }
}
