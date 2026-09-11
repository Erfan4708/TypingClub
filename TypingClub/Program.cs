using TypingClub.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

// Serve the game client (index.html, JavaScript, CSS and icons) from the wwwroot folder.
app.UseStaticFiles();

// Redirect the root URL to index.html, keeping the query string so invite links like "/?room=<id>" still work.
app.MapGet("/", (HttpContext context) => Results.Redirect("/index.html" + context.Request.QueryString));

// SignalR hub used by the client for all room and race messages.
app.MapHub<TypingHub>("/typingHub");

app.Run();
