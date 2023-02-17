using MQTTnet.AspNetCore;
using MQTTnet.AspNetCore.Controllers;
using MqttTest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMqttServer(options =>
{
    options.WithConnectionBacklog(1000);
    options.WithDefaultEndpoint();
    options.WithDefaultEndpointPort(1883);
});
builder.Services.AddMqttTcpServerAdapter();
builder.Services.AddMqttWebSocketServerAdapter();
builder.Services.AddConnections();

builder.Services.AddMqttControllers(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddMqttContextAccessor();
builder.Services.AddMqttAuthenticationController<MqttAuthenticationController>();
builder.Services.AddMqttConnectionController<MqttConnectionController>();

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
