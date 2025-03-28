namespace TypingClub.Models
{
    public class Room
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public required string Id { get; set; }
        public required string Text { get; set; }
        public Dictionary<string, int> Scores { get; set; } = new();
        public Dictionary<string, string> UserIcons { get; set; } = new();
        public List<string> AvailableIcons { get; set; } = new(); // Initialized in CreateRoom by cloning defaults.

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TimeoutMinutes { get; set; } = 10;
        public int MaxPlayers { get; set; } = 5;
        public string? WinningUser { get; set; }
        public TimeSpan? WinningTime { get; set; }
        public Dictionary<string, int> TypingSpeeds { get; set; } = new();
        public List<string> Spectators { get; set; } = new();
        public bool AllowSpectators { get; set; } = false;

        public enum RoomStatus { Waiting, InProgress, Completed, Expired }
        public RoomStatus Status { get; set; } = RoomStatus.Waiting;

        public enum Difficulty { Easy, Medium, Hard }
        public Difficulty DifficultyLevel { get; set; } = Difficulty.Medium;

        public string Language { get; set; } = "English";

        public bool IsActive => Status == RoomStatus.InProgress || Status == RoomStatus.Waiting;

        public void StartTimeout(TimeSpan timeout, Action<string> removeRoomCallback)
        {
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(timeout, _cancellationTokenSource.Token);
                    removeRoomCallback(Id);
                }
                catch (TaskCanceledException)
                {
                    // Timeout reset or room is still active.
                }
            });
        }

        public void ResetTimeout()
        {
            _cancellationTokenSource.Cancel();
        }
    }

}
