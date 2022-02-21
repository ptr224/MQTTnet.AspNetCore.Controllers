using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Transports.Mqtt
{
    internal sealed class Broker : IBroker, IHostedService, IDisposable
    {
        private readonly ILogger<Broker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMqttServerOptions mqttOptions;
        private readonly IMqttServer mqttServer;
        private readonly SemaphoreSlim _requestsQueue;
        private readonly RouteTable _routeTable;

        public Broker(ILogger<Broker> logger, IServiceScopeFactory scopeFactory, MqttBrokerOptions options, RouteTable routeTable, IBrokerAuthorizationPolicy authorizationPolicy = null, IBrokerConnectionHandler connectionHandler = null)
        {
            // Attiva policy se presente
            if (authorizationPolicy is not null)
                options
                    .WithConnectionValidator(authorizationPolicy)
                    .WithSubscriptionInterceptor(authorizationPolicy)
                    .WithApplicationMessageInterceptor(authorizationPolicy);

            mqttOptions = options.Build();
            mqttServer = new MqttFactory()
                .CreateMqttServer()
                .UseApplicationMessageReceivedHandler(MessageReceived);

            // Attiva handler connessioni se presente
            if (connectionHandler is not null)
                mqttServer
                    .UseClientConnectedHandler(connectionHandler)
                    .UseClientDisconnectedHandler(connectionHandler);

            _requestsQueue = new(options.MaxParallelRequests);

            _logger = logger;
            _scopeFactory = scopeFactory;
            _routeTable = routeTable;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return mqttServer.StartAsync(mqttOptions);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return mqttServer.StopAsync();
        }

        private async Task MessageReceived(MqttApplicationMessageReceivedEventArgs arg)
        {
            // Non processare i messaggi del broker
            if (arg.ClientId is null)
                return;

            // Attendi che si liberi la coda prima di lanciare l'handler
            await _requestsQueue.WaitAsync();
            _ = Task.Run(() => RequestHandler(arg));
        }

        private async Task RequestHandler(MqttApplicationMessageReceivedEventArgs arg)
        {
            try
            {
                // Controlla che il topic abbia un'azione corrispondente
                string[] topic = arg.ApplicationMessage.Topic.Split('/');
                var route = _routeTable.Match(topic);

                if (route is null)
                    return;

                // Crea lo scope, preleva il controller, imposta il contesto ed esegue l'azione richiesta con i parametri dati se presenti
                using var scope = _scopeFactory.CreateScope();
                var controller = scope.ServiceProvider.GetRequiredService(route.Method.DeclaringType);
                var parameters = route.Method.GetParameters();
                object result;

                (controller as MqttBaseController).Context = arg;

                if (parameters.Length == 0)
                {
                    result = route.Method.Invoke(controller, null);
                }
                else
                {
                    var paramsArray = new object[parameters.Length];
                    for (int i = 0; i < topic.Length; i++)
                    {
                        var segment = route.Template[i];
                        if (segment.IsParameter)
                        {
                            var info = segment.ParameterInfo;
                            paramsArray[info.Position] = info.ParameterType.IsEnum ? Enum.Parse(info.ParameterType, topic[i]) : Convert.ChangeType(topic[i], info.ParameterType);
                        }
                    }

                    result = route.Method.Invoke(controller, paramsArray);
                }

                // Se l'azione è un task aspettalo (sennò viene distrutto precocemente lo scope)
                if (result is Task awaitable)
                    await awaitable;
            }
            catch (TargetInvocationException e)
            {
                _logger.LogCritical(e.InnerException, $"Error in MQTT handler for '{arg.ApplicationMessage.Topic}': ");
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Error during MQTT handler activation for '{arg.ApplicationMessage.Topic}': ");
            }
            finally
            {
                // Libera la coda delle richieste
                _requestsQueue.Release();
            }
        }

        public Task Send(MqttApplicationMessage message, CancellationToken cancellationToken)
        {
            return mqttServer.PublishAsync(message, cancellationToken);
        }

        public void Dispose()
        {
            mqttServer.Dispose();
            _requestsQueue.Dispose();
        }
    }
}
