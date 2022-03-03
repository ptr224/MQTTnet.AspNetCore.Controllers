using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet.Extensions.Hosting.Routes;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Polly;
using Polly.Bulkhead;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.Hosting.Internals;

internal sealed class Broker :
    IMqttServerSubscriptionInterceptor,
    IMqttServerApplicationMessageInterceptor,
    IMqttServerConnectionValidator,
    IMqttServerClientConnectedHandler,
    IMqttServerClientDisconnectedHandler,
    IBroker,
    IHostedService,
    IDisposable
{
    private static object ActivateRoute(string[] topic, Route route, object controller)
    {
        var parameters = route.Method.GetParameters();

        if (parameters.Length == 0)
        {
            return route.Method.Invoke(controller, null);
        }
        else
        {
            var paramsArray = new object[parameters.Length];
            for (int i = 0; i < topic.Length; i++)
            {
                var segment = route.Template[i];
                if (segment.Type == SegmentType.Parametric)
                {
                    var info = segment.ParameterInfo;
                    paramsArray[info.Position] = info.ParameterType.IsEnum ? Enum.Parse(info.ParameterType, topic[i]) : Convert.ChangeType(topic[i], info.ParameterType);
                }
            }

            return route.Method.Invoke(controller, paramsArray);
        }
    }

    private readonly ILogger<Broker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMqttServerOptions mqttOptions;
    private readonly IMqttServer mqttServer;
    private readonly AsyncBulkheadPolicy policy;
    private readonly bool defaultSubscriptionAccept;
    private readonly bool defaultPublishAccept;
    private readonly SubscriptionRouteTable _subscriptionRouteTable;
    private readonly PublishRouteTable _publishRouteTable;

    public Broker(
        ILogger<Broker> logger,
        IServiceScopeFactory scopeFactory,
        MqttBrokerOptions options,
        SubscriptionRouteTable subscriptionRouteTable = null,
        PublishRouteTable publishRouteTable = null
        )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        mqttServer = new MqttFactory().CreateMqttServer();
        policy = Policy.BulkheadAsync(options.MaxParallelRequests, options.MaxParallelRequests * 4);
        defaultSubscriptionAccept = options.DefaultSubscriptionAccept;
        defaultPublishAccept = options.DefaultPublishAccept;

        // Attiva handler sottoscrizioni se necessario

        if (subscriptionRouteTable is not null)
        {
            options.WithSubscriptionInterceptor(this);
            _subscriptionRouteTable = subscriptionRouteTable;
        }

        // Attiva handler pubblicazioni se necessario

        if (publishRouteTable is not null)
        {
            options.WithApplicationMessageInterceptor(this);
            _publishRouteTable = publishRouteTable;
        }

        // Attiva handler autenticazione se necessario

        if (options.AuthenticationHandler is not null)
        {
            options.WithConnectionValidator(this);
        }

        // Attiva handler connessioni se necessario

        if (options.ConnectionHandler is not null)
        {
            mqttServer
                .UseClientConnectedHandler(this)
                .UseClientDisconnectedHandler(this);
        }

        // Finalizza

        mqttOptions = options.Build();
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var controller = scope.ServiceProvider.GetRequiredService(route.Method.DeclaringType);

            (controller as MqttSubscriptionController).Context = new(context);

            // Ritorna il risultato dell'operazione
            return ActivateRoute(topic, route, controller) switch
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var controller = scope.ServiceProvider.GetRequiredService(route.Method.DeclaringType);

            (controller as MqttPublishController).Context = new(context);

            // Ritorna il risultato dell'operazione
            var result = ActivateRoute(topic, route, controller) switch
            {
                IMqttPublishResult obj => obj,
                Task<IMqttPublishResult> task => await task,
                ValueTask<IMqttPublishResult> valueTask => await valueTask,
                _ => throw new NotSupportedException()
            };

            if (!result.Publish)
                context.ApplicationMessage = null;

            return result.Accept;
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

    private async Task<MqttConnectReasonCode> AuthenticationHandler(MqttConnectionValidatorContext context)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMqttAuthenticationHandler>();

            return await handler.AuthenticateAsync(new(context));
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, $"Error in MQTT authentication handler for '{context.ClientId}': ");
            return MqttConnectReasonCode.UnspecifiedError;
        }
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

    public async Task ValidateConnectionAsync(MqttConnectionValidatorContext context)
    {
        // Processa le autenticazioni dei client, annullandole se la coda è piena

        var result = await policy.ExecuteAndCaptureAsync(() => AuthenticationHandler(context));
        if (result.Outcome == OutcomeType.Successful)
        {
            context.ReasonCode = result.Result;
        }
        else
        {
            context.ReasonCode = MqttConnectReasonCode.ServerBusy;
            _logger.LogWarning($"Blocked authentication for '{context.ClientId}'");
        }
    }

    public async Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs eventArgs)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMqttConnectionHandler>();

            await handler.HandleClientConnectedAsync(eventArgs);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, $"Error in MQTT connection handler for '{eventArgs.ClientId}': ");
        }
    }

    public async Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMqttConnectionHandler>();

            await handler.HandleClientDisconnectedAsync(eventArgs);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, $"Error in MQTT disconnection handler for '{eventArgs.ClientId}': ");
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
