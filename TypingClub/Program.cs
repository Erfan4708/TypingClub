using TypingClub.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRouting();  // Add routing service (optional but good practice)
builder.Services.AddSignalR();  // Add SignalR services to the container

// Create the app
var app = builder.Build();

// Enable static file serving from the wwwroot folder
app.UseStaticFiles(); // This enables serving static files like HTML, CSS, JS from wwwroot

// Set up the default route to index.html
app.MapGet("/", async context =>
{
    context.Response.Redirect("/index.html"); // Redirect to index.html when the root is accessed
});

// Configure SignalR Hub
app.MapHub<TypingHub>("/typingHub"); // This maps the SignalR hub to the "/typingHub" endpoint

// Run the app
app.Run();
