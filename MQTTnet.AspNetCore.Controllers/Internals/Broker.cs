using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal sealed class Broker : IBroker
{
    private static async ValueTask ActivateRoute(string[] topic, Route route, object controller)
    {
        var parameters = route.Method.GetParameters();
        object returnValue;

        if (parameters.Length == 0)
        {
            returnValue = route.Method.Invoke(controller, null);
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

            returnValue = route.Method.Invoke(controller, paramsArray);
        }

        if (returnValue is Task task)
        {
            await task;
        }
        else if (returnValue is ValueTask valueTask)
        {
            await valueTask;
        }
    }

    private readonly ILogger<Broker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RouteTable _routeTable;
    private readonly bool hasAuthenticationHandler;
    private readonly bool hasConnectionHandler;

    private MqttServer mqttServer;

    public Broker(ILogger<Broker> logger, IServiceScopeFactory scopeFactory, MqttControllersOptions options, RouteTable routeTable = null)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _routeTable = routeTable;

        hasAuthenticationHandler = options.AuthenticationController is not null;
        hasConnectionHandler = options.ConnectionController is not null;
    }

    private async Task ValidatingConnectionAsync(ValidatingConnectionEventArgs context)
    {
        try
        {
            // Autentica client

            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMqttAuthenticationController>();

            await handler.AuthenticateAsync(context);
        }
        catch (Exception e)
        {
            context.ReasonCode = MqttConnectReasonCode.UnspecifiedError;
            _logger.LogCritical(e, "Error in MQTT authentication handler for '{ClientId}': ", context.ClientId);
        }
    }

    private async Task InterceptingPublishAsync(InterceptingPublishEventArgs context)
    {
        try
        {
            // Controlla che il topic abbia un'azione corrispondente

            string[] topic = context.ApplicationMessage.Topic.Split('/');
            var route = _routeTable.MatchPublish(topic);

            if (route is null)
            {
                context.ProcessPublish = false;
                context.Response.ReasonCode = MqttPubAckReasonCode.TopicNameInvalid;
            }
            else
            {
                // Crea scope, preleva controller, imposta contesto con valori di default ed attiva route

                await using var scope = _scopeFactory.CreateAsyncScope();
                var controller = scope.ServiceProvider.GetRequiredService(route.Method.DeclaringType);

                context.CloseConnection = false;
                context.ProcessPublish = true;
                (controller as MqttControllerBase).PublishContext = context;

                await ActivateRoute(topic, route, controller);
            }
        }
        catch (TargetInvocationException e)
        {
            context.ProcessPublish = false;
            context.Response.ReasonCode = MqttPubAckReasonCode.UnspecifiedError;
            _logger.LogCritical(e, "Error in MQTT publish handler for '{Topic}': ", context.ApplicationMessage.Topic);
        }
        catch (Exception e)
        {
            context.ProcessPublish = false;
            context.Response.ReasonCode = MqttPubAckReasonCode.UnspecifiedError;
            _logger.LogCritical(e, "Error during MQTT publish handler activation for '{Topic}': ", context.ApplicationMessage.Topic);
        }
    }

    private async Task InterceptingSubscriptionAsync(InterceptingSubscriptionEventArgs context)
    {
        try
        {
            // Controlla che il topic abbia un'azione corrispondente

            string[] topic = context.TopicFilter.Topic.Split('/');
            var route = _routeTable.MatchSubscribe(topic);

            if (route is null)
            {
                context.ProcessSubscription = false;
                context.Response.ReasonCode = MqttSubscribeReasonCode.TopicFilterInvalid;
            }
            else
            {
                // Crea scope, preleva controller, imposta contesto con valori di default ed attiva route

                await using var scope = _scopeFactory.CreateAsyncScope();
                var controller = scope.ServiceProvider.GetRequiredService(route.Method.DeclaringType);

                context.CloseConnection = false;
                context.ProcessSubscription = true;
                (controller as MqttControllerBase).SubscriptionContext = context;

                await ActivateRoute(topic, route, controller);
            }
        }
        catch (TargetInvocationException e)
        {
            context.ProcessSubscription = false;
            context.Response.ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
            _logger.LogCritical(e, "Error in MQTT subscription handler for '{topic}': ", context.TopicFilter.Topic);
        }
        catch (Exception e)
        {
            context.ProcessSubscription = false;
            context.Response.ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
            _logger.LogCritical(e, "Error during MQTT subscription handler activation for '{topic}': ", context.TopicFilter.Topic);
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
}
