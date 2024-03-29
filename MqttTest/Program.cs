using MQTTnet.AspNetCore;
using MQTTnet.AspNetCore.Controllers;
using MqttTest;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMqttServer();
//builder.Services.AddMqttTcpServerAdapter();
builder.Services.AddMqttWebSocketServerAdapter();
builder.Services.AddConnections();

builder.Services.AddMqttControllers(options =>
{
    options.AddAssembliesFromCurrentDomain();
    options.WithAuthenticationHandler<MqttAuthenticationHandler>();
    options.WithConnectionHandler<MqttTest.MqttConnectionHandler>();
    options.WithRetentionHandler<MqttRetentionHandler>();
    options.Filters.Add(new ActionFilter1Attribute());
    options.Binders.Add(new StringModelBinder1Attribute());
});
builder.Services.AddMqttContextAccessor();

builder.Services.AddScoped<MqttService>();
builder.Services.AddScoped<ActionFilterTest>();
builder.Services.AddScoped<ModelBinderTest>();

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
