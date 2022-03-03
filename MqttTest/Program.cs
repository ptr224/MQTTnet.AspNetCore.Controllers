using MqttTest.Services;
using MQTTnet.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMqttBroker(options =>
{
    options.WithConnectionBacklog(1000);
    options.WithDefaultEndpointPort(1883);
    options.WithMaxParallelRequests(4);
    options.WithControllers(AppDomain.CurrentDomain.GetAssemblies());
    options.WithAuthenticationHandler<MqttAuthenticationHandler>();
    options.WithConnectionHandler<MqttConnectionHandler>();
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

app.Run();
