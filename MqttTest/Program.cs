using MQTTnet.AspNetCore;
using MQTTnet.AspNetCore.Controllers;
using MqttTest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddMqttServer(options => options
        .WithConnectionBacklog(1000)
        .WithDefaultEndpoint()
        .WithDefaultEndpointPort(1883)
    )
    .AddMqttTcpServerAdapter()
    .AddMqttWebSocketServerAdapter()
    .AddConnections();

builder.Services
    .AddMqttControllers(AppDomain.CurrentDomain.GetAssemblies())
    .AddMqttContextAccessor()
    .AddMqttAuthenticationController<MqttAuthenticationController>()
    .AddMqttConnectionController<MqttConnectionController>();

builder.Services.AddScoped<MqttService>();

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
