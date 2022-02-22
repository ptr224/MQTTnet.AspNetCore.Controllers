using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server;
using Polly;
using Polly.Bulkhead;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.Hosting.Internals;

internal sealed class Broker : IMqttServerSubscriptionInterceptor, IMqttServerApplicationMessageInterceptor, IBroker, IHostedService, IDisposable
{
    private readonly ILogger<Broker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMqttServerOptions mqttOptions;
    private readonly IMqttServer mqttServer;
    private readonly AsyncBulkheadPolicy policy;
    private readonly bool defaultSubscriptionAccept;
    private readonly bool defaultPublishAccept;
    private readonly SubscriptionRouteTable _subscriptionRouteTable;
    private readonly PublishRouteTable _publishRouteTable;

    public Broker(ILogger<Broker> logger, IServiceScopeFactory scopeFactory, MqttBrokerOptions options, SubscriptionRouteTable subscriptionRouteTable, PublishRouteTable publishRouteTable, IBrokerConnectionHandler connectionHandler = null)
    {
        mqttServer = new MqttFactory().CreateMqttServer();

        // Attiva handler connessioni se presente
        if (connectionHandler is not null)
        {
            options
                .WithConnectionValidator(connectionHandler);

            mqttServer
                .UseClientConnectedHandler(connectionHandler)
                .UseClientDisconnectedHandler(connectionHandler);
        }

        mqttOptions = options
            .WithSubscriptionInterceptor(this)
            .WithApplicationMessageInterceptor(this)
            .Build();

        policy = Policy.BulkheadAsync(options.MaxParallelRequests, options.MaxParallelRequests * 4);
        defaultSubscriptionAccept = options.DefaultSubscriptionAccept;
        defaultPublishAccept = options.DefaultPublishAccept;

        _logger = logger;
        _scopeFactory = scopeFactory;
        _publishRouteTable = publishRouteTable;
        _subscriptionRouteTable = subscriptionRouteTable;
    }

    public async Task InterceptSubscriptionAsync(MqttSubscriptionInterceptorContext context)
    {
        if (context.ClientId is null)
        {
            // Accetta sempre le sottoscrizioni del broker
            context.AcceptSubscription = true;
        }
        else
        {
            // Processa le sottoscrizioni dei client, annullandole se la coda è piena
            var result = await policy.ExecuteAndCaptureAsync(() => SubscriptionHandler(context));
            if (result.Outcome == OutcomeType.Successful)
            {
                context.AcceptSubscription = result.Result;
            }
            else
            {
                context.AcceptSubscription = false;
                _logger.LogWarning($"Blocked subscription to '{context.TopicFilter.Topic}'");
            }
        }
    }

    public async Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
    {
        if (context.ClientId is null)
        {
            // Accetta sempre i messaggi del broker
            context.AcceptPublish = true;
        }
        else
        {
            // Processa le pubblicazioni dei client, annullandole se la coda è piena
            var result = await policy.ExecuteAndCaptureAsync(() => PublishHandler(context));
            if (result.Outcome == OutcomeType.Successful)
            {
                context.AcceptPublish = result.Result;
            }
            else
            {
                context.AcceptPublish = false;
                _logger.LogWarning($"Blocked publish to '{context.ApplicationMessage.Topic}'");
            }
        }
    }

    private async Task<bool> SubscriptionHandler(MqttSubscriptionInterceptorContext context)
    {
        try
        {
            // Controlla che il topic abbia un'azione corrispondente
            string[] topic = context.TopicFilter.Topic.Split('/');
            var route = _subscriptionRouteTable.Match(topic);

            if (route is null)
                return defaultSubscriptionAccept;

            // Crea lo scope, preleva il controller, imposta il contesto ed esegue l'azione richiesta con i parametri dati se presenti
            using var scope = _scopeFactory.CreateScope();
            var controller = scope.ServiceProvider.GetRequiredService(route.Method.DeclaringType);
            var parameters = route.Method.GetParameters();
            object result;

            (controller as MqttBaseSubscriptionController).Context = new(context);

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

            // Ritorna il risultato dell'operazione
            return result switch
            {
                bool obj => obj,
                Task<bool> task => await task,
                ValueTask<bool> valueTask => await valueTask,
                _ => throw new NotSupportedException()
            };
        }
        catch (TargetInvocationException e)
        {
            _logger.LogCritical(e.InnerException, $"Error in MQTT subscription handler for '{context.TopicFilter.Topic}': ");
            return false;
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, $"Error during MQTT subscription handler activation for '{context.TopicFilter.Topic}': ");
            return false;
        }
    }

    private async Task<bool> PublishHandler(MqttApplicationMessageInterceptorContext context)
    {
        try
        {
            // Controlla che il topic abbia un'azione corrispondente
            string[] topic = context.ApplicationMessage.Topic.Split('/');
            var route = _publishRouteTable.Match(topic);

            if (route is null)
                return defaultPublishAccept;

            // Crea lo scope, preleva il controller, imposta il contesto ed esegue l'azione richiesta con i parametri dati se presenti
            using var scope = _scopeFactory.CreateScope();
            var controller = scope.ServiceProvider.GetRequiredService(route.Method.DeclaringType);
            var parameters = route.Method.GetParameters();
            object result;

            (controller as MqttBasePublishController).Context = new(context);

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

            // Ritorna il risultato dell'operazione
            var pubResult = result switch
            {
                IMqttPublishResult obj => obj,
                Task<IMqttPublishResult> task => await task,
                ValueTask<IMqttPublishResult> valueTask => await valueTask,
                _ => throw new NotSupportedException()
            };

            if (!pubResult.Publish)
                context.ApplicationMessage = null;

            return pubResult.Accept;
        }
        catch (TargetInvocationException e)
        {
            _logger.LogCritical(e.InnerException, $"Error in MQTT publish handler for '{context.ApplicationMessage.Topic}': ");
            return false;
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, $"Error during MQTT publish handler activation for '{context.ApplicationMessage.Topic}': ");
            return false;
        }
    }

    public Task Send(MqttApplicationMessage message, CancellationToken cancellationToken)
    {
        return mqttServer.PublishAsync(message, cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return mqttServer.StartAsync(mqttOptions);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return mqttServer.StopAsync();
    }

    public void Dispose()
    {
        mqttServer.Dispose();
        policy.Dispose();
    }
}
