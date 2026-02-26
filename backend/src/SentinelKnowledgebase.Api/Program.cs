using FluentValidation.AspNetCore;
using SentinelKnowledgebase.Api.BackgroundProcessing;
using SentinelKnowledgebase.Application;
using SentinelKnowledgebase.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ICaptureProcessingQueue, CaptureProcessingQueue>();
builder.Services.AddHostedService<CaptureProcessingBackgroundService>();
builder.Services.AddFluentValidationAutoValidation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
