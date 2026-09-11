using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using TypingClub.Helpers;
using TypingClub.Models;

namespace TypingClub.Hubs
{
    public class TypingHub : Hub
    {
        // Hub instances are created per call, so rooms are kept in a static, in-memory dictionary.
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
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("Error", "Room not found. It may have expired.");
                return;
            }
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

            // Restart the inactivity timeout when a new user joins.
            room.StartTimeout(TimeSpan.FromMinutes(room.TimeoutMinutes), RemoveRoom);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("RoomJoined", room.Text, room.UserIcons);
            await Clients.Group(roomId).SendAsync("UserJoined", username, room.UserIcons[username]);
        }

        public async Task StartGame(string roomId)
        {
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("Error", "Room not found. It may have expired.");
                return;
            }

            // If a race was already played in this room, clear the old scores and pick a new paragraph.
            if (room.Scores.Any())
            {
                lock (room)
                {
                    room.Scores.Clear();
                }
                room.Text = TypingConstants.GetRandomParagraph();
                await Clients.Group(roomId).SendAsync("NewTextGenerated", room.Text);
            }
            room.Status = Room.RoomStatus.InProgress;

            // Starting a race also counts as activity.
            room.StartTimeout(TimeSpan.FromMinutes(room.TimeoutMinutes), RemoveRoom);

            await Clients.Group(roomId).SendAsync("StartCountdown");
        }

        public async Task UpdateProgress(string roomId, string username, int score)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                Dictionary<string, int> scores;
                lock (room)
                {
                    room.Scores[username] = score;
                    // Broadcast a copy so the dictionary isn't serialized while another call modifies it.
                    scores = new Dictionary<string, int>(room.Scores);
                }
                await Clients.Group(roomId).SendAsync("UpdateScores", scores);

                if (score >= room.Text.Length)
                {
                    // Finish times are measured by each client's own timer, so only the username is sent.
                    await Clients.Group(roomId).SendAsync("PlayerFinished", username);

                    // Once every player has finished, the room can accept new players again.
                    bool allFinished = room.UserIcons.Keys.All(user =>
                        scores.TryGetValue(user, out int userScore) && userScore >= room.Text.Length);
                    if (allFinished)
                    {
                        room.Status = Room.RoomStatus.Waiting;
                    }
                }
            }
        }

        // Static because it is invoked by the room's timer, long after this hub instance has been disposed.
        private static void RemoveRoom(string roomId)
        {
            if (Rooms.TryRemove(roomId, out _))
            {
                Console.WriteLine($"Room {roomId} has been removed due to inactivity.");
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
