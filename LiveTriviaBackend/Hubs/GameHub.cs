using Microsoft.AspNetCore.SignalR;
using live_trivia.Repositories;
using live_trivia.Data;

namespace live_trivia.Hubs
{
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

            // Notify other players that someone joined
            var game = await _gamesRepository.GetGameAsync(roomId);
            if (game != null)
            {
                await Clients.Group(roomId).SendAsync("PlayerJoined", new
                {
                    PlayerId = Context.ConnectionId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public async Task LeaveGameRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("PlayerLeft", Context.ConnectionId);
        }

        public async Task StartGame(string roomId)
        {
            var success = await _gamesRepository.StartGameAsync(roomId);
            if (success)
            {
                var gameDetails = await _gamesRepository.GetGameDetailsAsync(roomId);
                await Clients.Group(roomId).SendAsync("GameStarted", gameDetails);
            }
            else
            {
                await Clients.Caller.SendAsync("GameStartFailed", "Failed to start game");
            }
        }

        public async Task SubmitAnswer(string roomId, int questionId, List<int> selectedAnswers)
        {
            // Store the answer and notify others
            var game = await _gamesRepository.GetGameAsync(roomId);
            if (game != null)
            {
                await Clients.Group(roomId).SendAsync("AnswerSubmitted", new
                {
                    PlayerId = Context.ConnectionId,
                    QuestionId = questionId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public async Task NextQuestion(string roomId)
        {
            var game = await _gamesRepository.GetGameAsync(roomId);
            if (game != null)
            {
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
        }

        public async Task UpdateGameSettings(string roomId, object settings)
        {
            await Clients.Group(roomId).SendAsync("SettingsUpdated", settings);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
