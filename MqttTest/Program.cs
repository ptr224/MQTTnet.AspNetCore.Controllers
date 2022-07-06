using MQTTnet.AspNetCore;
using MQTTnet.AspNetCore.Controllers;
using MqttTest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
    .AddMqttTcpServerAdapter()
    .AddMqttWebSocketServerAdapter()
    .AddConnections();

builder.Services.AddMqttControllers(options =>
{
    options
        .WithMaxParallelRequests(4)
        .WithControllers(AppDomain.CurrentDomain.GetAssemblies())
        .WithAuthenticationController<MqttAuthenticationController>()
        .WithConnectionController<MqttConnectionController>();
});

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
