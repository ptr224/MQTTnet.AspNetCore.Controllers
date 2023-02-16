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
        object? returnValue;

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
                    var info = segment.ParameterInfo!;
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
    private readonly IMqttContextAccessor? mqttContextAccessor;
    private readonly bool hasAuthenticationController;
    private readonly bool hasConnectionController;
    private readonly string serverId;

    private MqttServer? mqttServer;

    public Broker(
        ILogger<Broker> logger,
        IServiceScopeFactory scopeFactory,
        RouteTable routeTable,
        IServiceProvider serviceProvider,
        IServiceProviderIsService isService
    )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _routeTable = routeTable;

        mqttContextAccessor = serviceProvider.GetService<IMqttContextAccessor>();
        hasAuthenticationController = isService.IsService(typeof(IMqttAuthenticationController));
        hasConnectionController = isService.IsService(typeof(IMqttConnectionController));
        serverId = Guid.NewGuid().ToString("N");
    }

    private async Task ValidatingConnectionAsync(ValidatingConnectionEventArgs context)
    {
        try
        {
            if (context.ClientId == serverId)
            {
                // Impedisci accesso se stesso ID del server

                context.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
            }
            else
            {
                // Autentica client

                await using var scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IMqttAuthenticationController>();

                await handler.AuthenticateAsync(context);
            }
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
            // Setta contesto

            if (mqttContextAccessor is not null)
                mqttContextAccessor.PublishContext = context;

            // Ignora i messaggi del server

            if (context.ClientId == serverId)
                return;

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
                // Imposta contesto con valori di default

                context.CloseConnection = false;
                context.ProcessPublish = true;

                // Crea scope, preleva controller, assegna contesto ed attiva route

                await using var scope = _scopeFactory.CreateAsyncScope();
                var controller = scope.ServiceProvider.GetRequiredService(route.Method.DeclaringType!);
                (controller as MqttControllerBase)!.ControllerContext = new()
                {
                    PublishEventArgs = context
                };

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
        finally
        {
            // Resetta contesto

            if (mqttContextAccessor is not null)
                mqttContextAccessor.PublishContext = null;
        }
    }

    private async Task InterceptingSubscriptionAsync(InterceptingSubscriptionEventArgs context)
    {
        try
        {
            // Setta contesto

            if (mqttContextAccessor is not null)
                mqttContextAccessor.SubscriptionContext = context;

            // Ignora i messaggi del server

            if (context.ClientId == serverId)
                return;

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
                // Imposta contesto con valori di default

                context.CloseConnection = false;
                context.ProcessSubscription = true;

                // Crea scope, preleva controller, assegna contesto ed attiva route

                await using var scope = _scopeFactory.CreateAsyncScope();
                var controller = scope.ServiceProvider.GetRequiredService(route.Method.DeclaringType!);
                (controller as MqttControllerBase)!.ControllerContext = new()
                {
                    SubscriptionEventArgs = context
                };

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
        finally
        {
            // Resetta contesto

            if (mqttContextAccessor is not null)
                mqttContextAccessor.SubscriptionContext = null;
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

        // Attiva handler pubblicazioni e sottoscrizioni

        server.InterceptingPublishAsync += InterceptingPublishAsync;
        server.InterceptingSubscriptionAsync += InterceptingSubscriptionAsync;

        // Attiva handler autenticazione se necessario

        if (hasAuthenticationController)
        {
            server.ValidatingConnectionAsync += ValidatingConnectionAsync;
        }

        // Attiva handler connessioni se necessario

        if (hasConnectionController)
        {
            server.ClientConnectedAsync += ClientConnectedAsync;
            server.ClientDisconnectedAsync += ClientDisconnectedAsync;
        }
    }

    public Task Send(MqttApplicationMessage message)
    {
        return mqttServer!.InjectApplicationMessage(new(message)
        {
            SenderClientId = serverId
        });
    }
}
