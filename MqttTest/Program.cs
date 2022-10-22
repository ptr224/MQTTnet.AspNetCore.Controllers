using MQTTnet.AspNetCore;
using MQTTnet.AspNetCore.Controllers;
using MqttTest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddMqttServer(options =>
    {
        options
            .WithConnectionBacklog(1000)
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(1883);
    })
    .AddMqttControllers(options =>
    {
        options
            .WithControllers(AppDomain.CurrentDomain.GetAssemblies())
            .WithAuthenticationController<MqttAuthenticationController>()
            .WithConnectionController<MqttConnectionController>();
    })
    .AddMqttTcpServerAdapter()
    .AddMqttWebSocketServerAdapter()
    .AddConnections();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
app.MapMqtt("/mqtt");

app.UseMqttControllers();

app.Run();
