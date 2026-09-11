namespace TypingClub.Models
{
    public class Room
    {
        private CancellationTokenSource _timeoutTokenSource = new();

        public required string Id { get; set; }
        public required string Text { get; set; }

        // Number of correctly typed characters per username.
        public Dictionary<string, int> Scores { get; set; } = new();

        // Icon file name (from wwwroot/images) assigned to each username.
        public Dictionary<string, string> UserIcons { get; set; } = new();

        // Icons not yet assigned in this room. Initialized in CreateRoom by cloning the defaults.
        public List<string> AvailableIcons { get; set; } = new();

        public int TimeoutMinutes { get; set; } = 10;

        public enum RoomStatus { Waiting, InProgress }
        public RoomStatus Status { get; set; } = RoomStatus.Waiting;

        /// <summary>
        /// Starts (or restarts) the inactivity timer. Any previously started timer is cancelled,
        /// so the room is only removed if no new timer is started within <paramref name="timeout"/>.
        /// </summary>
        public void StartTimeout(TimeSpan timeout, Action<string> removeRoomCallback)
        {
            CancellationToken token;
            lock (this)
            {
                // A cancelled token source can't be reused, so every timer gets a fresh one.
                _timeoutTokenSource.Cancel();
                _timeoutTokenSource = new CancellationTokenSource();
                token = _timeoutTokenSource.Token;
            }

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(timeout, token);
                    removeRoomCallback(Id);
                }
                catch (TaskCanceledException)
                {
                    // A newer timer replaced this one.
                }
            });
        }
    }
}
