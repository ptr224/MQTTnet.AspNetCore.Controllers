using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.AspNetCore.Controllers.Routes;
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
    private readonly SubscriptionRouteTable _subscriptionRouteTable;
    private readonly PublishRouteTable _publishRouteTable;
    private readonly AsyncBulkheadPolicy policy;
    private readonly bool hasAuthenticationHandler;
    private readonly bool hasConnectionHandler;

    private MqttServer mqttServer;

    public Broker(
        ILogger<Broker> logger,
        IServiceScopeFactory scopeFactory,
        MqttControllersOptions options,
        SubscriptionRouteTable subscriptionRouteTable = null,
        PublishRouteTable publishRouteTable = null
        )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _subscriptionRouteTable = subscriptionRouteTable;
        _publishRouteTable = publishRouteTable;

        policy = Policy.BulkheadAsync(options.MaxParallelRequests, options.MaxParallelRequests * 4);
        hasAuthenticationHandler = options.AuthenticationController is not null;
        hasConnectionHandler = options.ConnectionController is not null;
    }

    private async Task<MqttConnectReasonCode> AuthenticationHandler(ValidatingConnectionEventArgs context)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMqttAuthenticationController>();

            return await handler.AuthenticateAsync(new(context));
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error in MQTT authentication handler for '{ClientId}': ", context.ClientId);
            return MqttConnectReasonCode.UnspecifiedError;
        }
    }

    private async Task ValidatingConnectionAsync(ValidatingConnectionEventArgs context)
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
            _logger.LogWarning("Blocked authentication for '{ClientId}'", context.ClientId);
        }
    }

    private async Task<bool> SubscriptionHandler(InterceptingSubscriptionEventArgs context)
    {
        try
        {
            // Controlla che il topic abbia un'azione corrispondente

            string[] topic = context.TopicFilter.Topic.Split('/');
            var route = _subscriptionRouteTable.Match(topic);

            if (route is null)
                return false;

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
            _logger.LogCritical(e.InnerException, "Error in MQTT subscription handler for '{topic}': ", context.TopicFilter.Topic);
            return false;
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error during MQTT subscription handler activation for '{topic}': ", context.TopicFilter.Topic);
            return false;
        }
    }

    private async Task InterceptingSubscriptionAsync(InterceptingSubscriptionEventArgs context)
    {
        // Processa le sottoscrizioni dei client, annullandole se la coda è piena

        var result = await policy.ExecuteAndCaptureAsync(() => SubscriptionHandler(context));
        if (result.Outcome == OutcomeType.Successful)
        {
            context.ProcessSubscription = result.Result;
        }
        else
        {
            context.ProcessSubscription = false;
            _logger.LogWarning("Blocked subscription to '{Topic}'", context.TopicFilter.Topic);
        }
    }

    private async Task<bool> PublishHandler(InterceptingPublishEventArgs context)
    {
        try
        {
            // Controlla che il topic abbia un'azione corrispondente

            string[] topic = context.ApplicationMessage.Topic.Split('/');
            var route = _publishRouteTable.Match(topic);

            if (route is null)
                return false;

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
            _logger.LogCritical(e.InnerException, "Error in MQTT publish handler for '{Topic}': ", context.ApplicationMessage.Topic);
            return false;
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error during MQTT publish handler activation for '{Topic}': ", context.ApplicationMessage.Topic);
            return false;
        }
    }

    private async Task InterceptingPublishAsync(InterceptingPublishEventArgs context)
    {
        // Processa le pubblicazioni dei client, annullandole se la coda è piena

        var result = await policy.ExecuteAndCaptureAsync(() => PublishHandler(context));
        if (result.Outcome == OutcomeType.Successful)
        {
            context.ProcessPublish = result.Result;
        }
        else
        {
            context.ProcessPublish = false;
            _logger.LogWarning("Blocked publish to '{Topic}'", context.ApplicationMessage.Topic);
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

        // Attiva handler sottoscrizioni se necessario

        if (_subscriptionRouteTable is not null)
        {
            server.InterceptingSubscriptionAsync += InterceptingSubscriptionAsync;
        }

        // Attiva handler pubblicazioni se necessario

        if (_publishRouteTable is not null)
        {
            server.InterceptingPublishAsync += InterceptingPublishAsync;
        }

        // Attiva handler autenticazione se necessario

        if (hasAuthenticationHandler)
        {
            server.ValidatingConnectionAsync += ValidatingConnectionAsync;
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
