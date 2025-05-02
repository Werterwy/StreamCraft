using Microsoft.EntityFrameworkCore;
using Serilog;
using StreamCraftAPI;
using StreamCraftAPI.Data.DbContext;
using StreamCraftAPI.Interface;
using StreamCraftAPI.Queues;
using StreamCraftAPI.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StreamCraftDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Настройка Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341") 
    .Enrich.FromLogContext()
);

builder.Services.AddScoped<VideoStorageService>();

builder.Services.AddSingleton<VideoProcessingQueue>();

builder.Services.AddHostedService<VideoProcessingService>();

builder.Services.AddSingleton<WebSocketConnectionManager>();


builder.Services.AddSingleton<IPermanentStorageService>(provider =>
{
    var s3 = provider.GetRequiredService<S3StorageService>();
    var fallback = provider.GetRequiredService<LocalFallbackStorageService>();
    return new ResilientStorageService(s3, fallback);
});
builder.Services.AddSingleton<S3StorageService>();
builder.Services.AddSingleton<LocalFallbackStorageService>();
builder.Services.AddScoped<StorageOrchestratorService>();


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseWebSockets();

app.Map("/ws/notifications", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var userId = context.Request.Query["userId"].ToString();

        if (!string.IsNullOrEmpty(userId))
        {
            var connectionManager = context.RequestServices.GetRequiredService<WebSocketConnectionManager>();
            await connectionManager.Register(userId, webSocket);
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.UseAuthorization();

app.MapControllers();

app.Run();
