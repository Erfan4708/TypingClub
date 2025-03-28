using TypingClub.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRouting();  // Optional but good practice
builder.Services.AddSignalR();  // Register SignalR services

// Create the app
var app = builder.Build();

// Enable static file serving from the wwwroot folder.
app.UseStaticFiles();

// Redirect the root URL to index.html.
// If a room parameter is present, pass it along in the query string.
app.MapGet("/", async context =>
{
    var room = context.Request.Query["room"].ToString();
    if (!string.IsNullOrEmpty(room))
    {
        context.Response.Redirect($"/index.html?room={room}");
    }
    else
    {
        context.Response.Redirect("/index.html");
    }
});

// Configure the SignalR hub endpoint.
app.MapHub<TypingHub>("/typingHub");

// Run the app.
app.Run();
