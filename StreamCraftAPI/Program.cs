using Microsoft.EntityFrameworkCore;
using Serilog;
using StreamCraftAPI.Data.DbContext;
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

builder.Services.AddHostedService<VideoProcessingWorker>();



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

app.UseAuthorization();

app.MapControllers();

app.Run();
