using Amazon.S3;
using Amazon;
using CorrelationId;
using CorrelationId.DependencyInjection;
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

/*// Настройка Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341") 
    .Enrich.FromLogContext()
);*/

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Async(a => a.Seq("http://localhost:5341")) 
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<VideoStorageService>();

builder.Services.AddSingleton<VideoProcessingQueue>();

builder.Services.AddScoped<VideoProcessingService>();

builder.Services.AddSingleton<WebSocketConnectionManager>();

builder.Services.AddDefaultCorrelationId();

builder.Services.AddHealthChecks();


builder.Services.AddSingleton<IPermanentStorageService>(provider =>
{
    var s3 = provider.GetRequiredService<S3StorageService>();
    var fallback = provider.GetRequiredService<LocalFallbackStorageService>();
    return new ResilientStorageService(s3, fallback);
});

builder.Services.AddSingleton<S3StorageService>();
builder.Services.AddSingleton<LocalFallbackStorageService>();
builder.Services.AddScoped<StorageOrchestratorService>();

builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(
    new AmazonS3Config
    {
        RegionEndpoint = RegionEndpoint.USEast1 
    }
));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:44328") 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

app.UseCors("AllowFrontend");


app.MapHealthChecks("/health");

app.UseCorrelationId();

app.Use(async (context, next) =>
{
    var requestId = context.TraceIdentifier;
    Log.Information("HTTP {Method} {Path} - RequestId: {RequestId}",
        context.Request.Method, context.Request.Path, requestId);

    await next();

    Log.Information("Response {StatusCode} - RequestId: {RequestId}",
        context.Response.StatusCode, requestId);
});


app.UseWebSockets();

app.UseAuthorization();

app.MapControllers();

app.Run();
