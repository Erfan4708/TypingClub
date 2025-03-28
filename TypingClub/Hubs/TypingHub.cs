using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using TypingClub.Helpers;
using TypingClub.Models;

namespace TypingClub.Hubs
{
    public class TypingHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Room> Rooms = new();

        public async Task CreateRoom(string username)
        {
            string roomId = Guid.NewGuid().ToString();
            string paragraph = TypingConstants.GetRandomParagraph();
            var room = new Room
            {
                Id = roomId,
                Text = paragraph,
                // Clone the default icons list so each room gets its own copy.
                AvailableIcons = new List<string>(TypingConstants.DefaultAvailableIcons)
            };

            AssignUserIcon(room, username);
            room.Status = Room.RoomStatus.Waiting;
            Rooms[roomId] = room;

            room.StartTimeout(TimeSpan.FromMinutes(room.TimeoutMinutes), RemoveRoom);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("RoomCreated", roomId, paragraph, room.UserIcons);
        }

        public async Task JoinRoom(string roomId, string username)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                if (room.Status != Room.RoomStatus.Waiting)
                {
                    await Clients.Caller.SendAsync("Error", "Room is busy.");
                    return;
                }
                if (room.UserIcons.ContainsKey(username))
                {
                    await Clients.Caller.SendAsync("Error", "Username already taken in this room.");
                    return;
                }

                AssignUserIcon(room, username);

                // Reset timeout when a new user joins.
                room.ResetTimeout();
                room.StartTimeout(TimeSpan.FromMinutes(room.TimeoutMinutes), RemoveRoom);

                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Caller.SendAsync("RoomJoined", room.Text, room.UserIcons);
                await Clients.Group(roomId).SendAsync("UserJoined", username, room.UserIcons[username]);
            }
        }

        public async Task StartGame(string roomId)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                // If scores already exist, generate new paragraph.
                if (room.Scores.Any())
                {
                    room.Text = TypingConstants.GetRandomParagraph();
                    await Clients.Group(roomId).SendAsync("NewTextGenerated", room.Text);
                }
                room.Status = Room.RoomStatus.InProgress;
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
                    // For simplicity, client sends the time; in production, track server-side.
                    await Clients.Group(roomId).SendAsync("WinnerAnnounced", username, 0); // Placeholder time
                }
            }
        }

        private void RemoveRoom(string roomId)
        {
            if (Rooms.TryRemove(roomId, out _))
            {
                Console.WriteLine($"Room {roomId} has been disposed due to inactivity.");
            }
        }

        /// <summary>
        /// Assigns an available icon to a user within a room.
        /// </summary>
        /// <param name="room">The room instance.</param>
        /// <param name="username">The username to assign the icon.</param>
        private void AssignUserIcon(Room room, string username)
        {
            lock (room)
            {
                var random = new Random();
                if (room.AvailableIcons.Any())
                {
                    int randomIndex = random.Next(room.AvailableIcons.Count);
                    string icon = room.AvailableIcons[randomIndex];
                    room.UserIcons[username] = icon;
                    room.AvailableIcons.RemoveAt(randomIndex);
                }
                else
                {
                    room.UserIcons[username] = "image1.png"; // Fallback icon.
                }
            }
        }
    }
}
