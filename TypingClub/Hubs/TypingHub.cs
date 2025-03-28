using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TypingClub.Hubs
{
    public class TypingHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Room> Rooms = new();

        public async Task CreateRoom(string username)
        {
            string roomId = Guid.NewGuid().ToString();
            var paragraph = GenerateRandomParagraph();
            var room = new Room { Id = roomId, Text = paragraph };

            lock (room)
            {
                if (room.AvailableIcons.Any())
                {
                    var random = new Random();
                    int randomIndex = random.Next(room.AvailableIcons.Count);
                    string icon = room.AvailableIcons[randomIndex];
                    room.UserIcons[username] = icon;
                    room.AvailableIcons.Remove(icon);
                }
                else
                {
                    room.UserIcons[username] = "image1.png"; // Fallback
                }
            }

            Rooms[roomId] = room;

            // Start timeout countdown (e.g., dispose after 10 minutes)
            room.StartTimeout(TimeSpan.FromMinutes(10), RemoveRoom);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("RoomCreated", roomId, paragraph, room.UserIcons);
        }

        private void RemoveRoom(string roomId)
        {
            if (Rooms.TryRemove(roomId, out _))
            {
                Console.WriteLine($"Room {roomId} has been disposed due to inactivity.");
            }
        }


        public async Task JoinRoom(string roomId, string username)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                if (room.UserIcons.ContainsKey(username))
                {
                    await Clients.Caller.SendAsync("Error", "Username already taken in this room.");
                    return;
                }

                lock (room)
                {
                    if (room.AvailableIcons.Any())
                    {
                        var random = new Random();
                        int randomIndex = random.Next(room.AvailableIcons.Count);
                        string icon = room.AvailableIcons[randomIndex];
                        room.UserIcons[username] = icon;
                        room.AvailableIcons.Remove(icon);
                    }
                    else
                    {
                        room.UserIcons[username] = "image1.png"; // Fallback
                    }
                }

                // Reset timeout when a new user joins
                room.ResetTimeout();
                room.StartTimeout(TimeSpan.FromMinutes(10), RemoveRoom);

                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Caller.SendAsync("RoomJoined", room.Text, room.UserIcons);
                await Clients.Group(roomId).SendAsync("UserJoined", username, room.UserIcons[username]);
            }
        }


        public async Task StartGame(string roomId)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                if (Rooms[roomId].Scores.Any())
                {
                    Rooms[roomId].Text = GenerateRandomParagraph();
                    await Clients.Group(roomId).SendAsync("NewTextGenerated", Rooms[roomId].Text);
                }
                await Clients.Group(roomId).SendAsync("StartCountdown");
            }
        }

        public async Task UpdateProgress(string roomId, string username, int score)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                lock (room)
                {
                    room.Scores[username] = score;
                }
                await Clients.Group(roomId).SendAsync("UpdateScores", room.Scores);
                if (score >= room.Text.Length)
                {
                    // For simplicity, let the client send the time; in production, track it server-side
                    await Clients.Group(roomId).SendAsync("WinnerAnnounced", username, 0); // Placeholder time
                }
            }
        }

        private string GenerateRandomParagraph()
        {
            string[] paragraphs =
            {
                // Original paragraphs
                "This is a longer paragraph for the typing race game. It includes more text to make the game challenging and fun. Users will need to type this entire paragraph correctly to win the race. The quick brown fox jumps over the lazy dog, and the race continues with more sentences to test their typing speed and accuracy.",
                "Another extended paragraph for the typing race. This one is designed to be even longer, providing a greater challenge for participants. Typing accurately and quickly is key to winning. Practice makes perfect, and with each race, users can improve their skills. The race is on, and only the fastest typist will emerge victorious.",

                // Funny paragraphs
                "Life is a carnival of mishaps, where even the simplest typo can spark a burst of laughter. Imagine a keyboard that chuckles with every keystroke. In this race, speed meets humor, turning each mistake into a moment of joy.",
                "Sometimes the best part of typing is the unexpected words that come out when your fingers decide to have a mind of their own. Embrace the quirky chaos and let the typos remind you not to take things too seriously.",
                "Picture a world where every error writes its own punchline. As you race against the clock, every misplaced comma or rogue letter becomes part of a hilarious story unfolding right before your eyes.",
                "If your keyboard had a personality, it might tease you about every mistake. Each key press is a mini adventure in mischief and mayhem, turning the race into a comedic journey.",
                "In this game, every typo is an opportunity for laughter. Embrace the absurdity of a wandering finger on the keyboard and let each error create a quirky masterpiece.",

                // Literary / Inspired paragraphs
                "It was the best of times, it was the worst of times. Inspired by classic literature, this passage evokes the grandeur of timeless works, challenging participants to channel their inner author as they race against the clock.",
                "As the pen scratches the paper in an endless dance of words, so do your fingers tap in a rhythmic race. Embrace the literary challenge as you traverse a passage rich with history and style.",
                "Amidst the gentle hum of whispered legends and the rustle of ancient pages, lies a story waiting to be typed. Let your words flow as gracefully as a sonnet from a bygone era.",
                "In a realm of words and dreams, every letter tells a story. Let your fingers weave an epic saga as you transform keystrokes into timeless adventures.",
                "As the rhythm of the keys echoes the beats of a timeless verse, immerse yourself in the dance of letters and let each word paint a vivid picture.",

                // Motivational / Historical / Tech-themed paragraphs
                "Your keystrokes are like brushstrokes on the canvas of success. With every word typed, you're creating a masterpiece of determination and skill.",
                "Imagine the great scribes of old, etching their thoughts onto parchment with quills. Now, you carry that legacy forward with every keystroke you make.",
                "In the digital realm where speed meets precision, every keystroke is a command and every error a chance to debug your strategy. Embrace the efficiency of modern typing.",
                "Each tap on your keyboard is a step towards excellence. Let the rhythm of your fingers propel you towards achieving greatness in this race.",
                "Channel the spirit of legendary authors and pioneers. As you type, feel the weight of history and the promise of future innovation."
            };

            var random = new Random();
            return paragraphs[random.Next(paragraphs.Length)];
        }


    }

    public class Room
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public string Id { get; set; }
        public string Text { get; set; }
        public Dictionary<string, int> Scores { get; set; } = new();
        public Dictionary<string, string> UserIcons { get; set; } = new();
        public List<string> AvailableIcons { get; set; } = new()
        {
            "image1.png", "image6.png",
            "image7.png", "image8.png", "image9.png", "image10.png",
            "image11.png", "image12.png", "image13.png", "image14.png",
            "image15.png", "image16.png", "image17.png", "image18.png",
            "image19.png",
        };

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
                    // Timeout reset or room is still active
                }
            });
        }

        public void ResetTimeout()
        {
            _cancellationTokenSource.Cancel();
        }
    }

}