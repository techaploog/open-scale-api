using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using fpn_scale_api.Services;
using fpn_scale_api.Helpers;
using fpn_scale_api.Middleware;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add logging
var logger = LoggerFactory.Create(loggingBuilder =>
{
  loggingBuilder.AddConsole();
}).CreateLogger("ConfigLogger");

try
  {
  // Read configuration from text file
  var configPath = Path.Combine(AppContext.BaseDirectory, "config.txt");
  var config = ConfigReader.ReadConfig(configPath, logger);

  // Parse the UUID to COM port mapping
  var portMapping = ConfigReader.ParsePortMapping(config["PortMapping"]);

  // Add services to the container.
  builder.Services.AddControllers();
  builder.Services.AddSingleton(portMapping);
  builder.Services.AddSingleton(new SerialPortSettings
    {
    SampleSize = int.Parse(config["SampleSize"]),
    TimeoutMilliseconds = int.Parse(config["TimeoutMilliseconds"]),
    DefaultBaudRate = int.Parse(config["DefaultBaudRate"]),
    ErrorTolerance = double.Parse(config["ErrorTolerance"]),
    HttpUrl = config["HttpUrl"],
    StandardDataPattern = config["StandardDataPattern"]
    });

  builder.Services.AddSingleton<SerialPortService>();

  builder.Services.Configure<KestrelServerOptions>(options =>
  {
    options.ListenAnyIP(new Uri(config["HttpUrl"]).Port);
  });
  builder.Services.AddSwaggerGen();

  var app = builder.Build();

  // Configure the HTTP request pipeline.
  if (app.Environment.IsDevelopment())
    {
    app.UseSwagger();
    app.UseSwaggerUI();
    }

  // Register the custom exception handling middleware
  app.UseMiddleware<CustomExceptionHandlerMiddleware>();

  app.UseHttpsRedirection();
  app.UseAuthorization();
  app.MapControllers();

  app.Run(config["HttpUrl"]);
  }
catch (Exception ex)
  {
  logger.LogError("[ERROR] Application terminated due to configuration error: {message}", ex.Message);
  Environment.Exit(1);
  }
