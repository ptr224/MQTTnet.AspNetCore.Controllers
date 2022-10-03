using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Polly;
using Polly.Bulkhead;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal sealed class Broker : IBroker, IDisposable
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
            for (int i = 0; i < route.Template.Length; i++)
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
    private readonly RouteTable _routeTable;
    private readonly AsyncBulkheadPolicy policy;
    private readonly bool hasAuthenticationHandler;
    private readonly bool hasConnectionHandler;

    private MqttServer mqttServer;

    public Broker(ILogger<Broker> logger, IServiceScopeFactory scopeFactory, MqttControllersOptions options, RouteTable routeTable = null)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _routeTable = routeTable;

        policy = Policy.BulkheadAsync(options.MaxParallelRequests, options.MaxParallelRequests * 4);
        hasAuthenticationHandler = options.AuthenticationController is not null;
        hasConnectionHandler = options.ConnectionController is not null;
    }

    private async Task ValidatingConnectionAsync(ValidatingConnectionEventArgs context)
    {
        // Processa le autenticazioni dei client, annullandole se la coda è piena

        var result = await policy.ExecuteAndCaptureAsync(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMqttAuthenticationController>();

            await handler.AuthenticateAsync(context);
        });

        if (result.Outcome == OutcomeType.Failure)
        {
            switch (result.FinalException)
            {
                case null:
                    context.ReasonCode = MqttConnectReasonCode.ServerBusy;
                    _logger.LogWarning("Blocked authentication for '{ClientId}'", context.ClientId);
                    break;
                default:
                    context.ReasonCode = MqttConnectReasonCode.UnspecifiedError;
                    _logger.LogCritical(result.FinalException, "Error in MQTT authentication handler for '{ClientId}': ", context.ClientId);
                    break;
            }
        }
    }

    private async Task InterceptingPublishAsync(InterceptingPublishEventArgs context)
    {
        // Processa le pubblicazioni dei client, annullandole se la coda è piena

        var result = await policy.ExecuteAndCaptureAsync(async () =>
        {
            // Controlla che il topic abbia un'azione corrispondente

            string[] topic = context.ApplicationMessage.Topic.Split('/');
            var route = _routeTable.MatchPublish(topic);

            if (route is null)
            {
                context.ProcessPublish = false;
                context.Response.ReasonCode = MqttPubAckReasonCode.TopicNameInvalid;
                return;
            }

            // Crea lo scope, preleva il controller, imposta il contesto ed esegue l'azione richiesta con i parametri dati se presenti

            await using var scope = _scopeFactory.CreateAsyncScope();
            var controller = scope.ServiceProvider.GetRequiredService(route.Method.DeclaringType);

            context.CloseConnection = false;
            context.ProcessPublish = true;
            (controller as MqttControllerBase).PublishContext = context;

            // Attiva route ed eventualmente attendi

            var obj = ActivateRoute(topic, route, controller);

            if (obj is Task task)
            {
                await task;
            }
            else if (obj is ValueTask valueTask)
            {
                await valueTask;
            }
        });

        if (result.Outcome == OutcomeType.Failure)
        {
            context.ProcessPublish = false;

            switch (result.FinalException)
            {
                case null:
                    context.Response.ReasonCode = MqttPubAckReasonCode.QuotaExceeded;
                    _logger.LogWarning("Blocked publish to '{Topic}'", context.ApplicationMessage.Topic);
                    break;
                case TargetInvocationException tie:
                    context.Response.ReasonCode = MqttPubAckReasonCode.UnspecifiedError;
                    _logger.LogCritical(tie.InnerException, "Error in MQTT publish handler for '{Topic}': ", context.ApplicationMessage.Topic);
                    break;
                default:
                    context.Response.ReasonCode = MqttPubAckReasonCode.UnspecifiedError;
                    _logger.LogCritical(result.FinalException, "Error during MQTT publish handler activation for '{Topic}': ", context.ApplicationMessage.Topic);
                    break;
            }
        }
    }

    private async Task InterceptingSubscriptionAsync(InterceptingSubscriptionEventArgs context)
    {
        // Processa le sottoscrizioni dei client, annullandole se la coda è piena

        var result = await policy.ExecuteAndCaptureAsync(async () =>
        {
            // Controlla che il topic abbia un'azione corrispondente

            string[] topic = context.TopicFilter.Topic.Split('/');
            var route = _routeTable.MatchSubscribe(topic);

            if (route is null)
            {
                context.ProcessSubscription = false;
                context.Response.ReasonCode = MqttSubscribeReasonCode.TopicFilterInvalid;
                return;
            }

            // Crea lo scope, preleva il controller, imposta il contesto ed esegue l'azione richiesta con i parametri dati se presenti

            await using var scope = _scopeFactory.CreateAsyncScope();
            var controller = scope.ServiceProvider.GetRequiredService(route.Method.DeclaringType);

            context.CloseConnection = false;
            context.ProcessSubscription = true;
            (controller as MqttControllerBase).SubscriptionContext = context;

            // Attiva route ed eventualmente attendi

            var obj = ActivateRoute(topic, route, controller);

            if (obj is Task task)
            {
                await task;
            }
            else if (obj is ValueTask valueTask)
            {
                await valueTask;
            }
        });

        if (result.Outcome == OutcomeType.Failure)
        {
            context.ProcessSubscription = false;

            switch (result.FinalException)
            {
                case null:
                    context.Response.ReasonCode = MqttSubscribeReasonCode.QuotaExceeded;
                    _logger.LogWarning("Blocked subscription to '{Topic}'", context.TopicFilter.Topic);
                    break;
                case TargetInvocationException tie:
                    context.Response.ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
                    _logger.LogCritical(tie.InnerException, "Error in MQTT subscription handler for '{topic}': ", context.TopicFilter.Topic);
                    break;
                default:
                    context.Response.ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
                    _logger.LogCritical(result.FinalException, "Error during MQTT subscription handler activation for '{topic}': ", context.TopicFilter.Topic);
                    break;
            }
        }
    }

    private async Task ClientConnectedAsync(ClientConnectedEventArgs eventArgs)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMqttConnectionController>();

            await handler.ClientConnectedAsync(eventArgs);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error in MQTT connection handler for '{ClientId}': ", eventArgs.ClientId);
        }
    }

    private async Task ClientDisconnectedAsync(ClientDisconnectedEventArgs eventArgs)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMqttConnectionController>();

            await handler.ClientDisconnectedAsync(eventArgs);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error in MQTT disconnection handler for '{ClientId}': ", eventArgs.ClientId);
        }
    }

    public void UseMqttServer(MqttServer server)
    {
        mqttServer = server;

        // Attiva handler autenticazione se necessario

        if (hasAuthenticationHandler)
        {
            server.ValidatingConnectionAsync += ValidatingConnectionAsync;
        }

        // Attiva handler pubblicazioni e sottoscrizioni se necessario

        if (_routeTable is not null)
        {
            server.InterceptingPublishAsync += InterceptingPublishAsync;
            server.InterceptingSubscriptionAsync += InterceptingSubscriptionAsync;
        }

        // Attiva handler connessioni se necessario

        if (hasConnectionHandler)
        {
            server.ClientConnectedAsync += ClientConnectedAsync;
            server.ClientDisconnectedAsync += ClientDisconnectedAsync;
        }
    }

    public Task Send(MqttApplicationMessage message)
    {
        return mqttServer.InjectApplicationMessage(new(message));
    }

    public void Dispose()
    {
        policy.Dispose();
    }
}
