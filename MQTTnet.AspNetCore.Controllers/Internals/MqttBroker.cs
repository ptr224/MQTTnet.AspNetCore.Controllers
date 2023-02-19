using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Controllers.Internals;

internal sealed class MqttBroker : IMqttBroker
{
    private readonly ILogger<MqttBroker> _logger;
    private readonly RouteTable _routeTable;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMqttContextAccessor? _mqttContextAccessor;
    private readonly string serverId;

    private MqttServer? mqttServer;

    public MqttBroker(ILogger<MqttBroker> logger, RouteTable routeTable, IServiceScopeFactory scopeFactory, IMqttContextAccessor? mqttContextAccessor = null)
    {
        _logger = logger;
        _routeTable = routeTable;
        _scopeFactory = scopeFactory;
        _mqttContextAccessor = mqttContextAccessor;

        serverId = Guid.NewGuid().ToString("N");
    }

    private async Task InterceptingPublishAsync(InterceptingPublishEventArgs args)
    {
        try
        {
            // Setta contesto

            if (_mqttContextAccessor is not null)
                _mqttContextAccessor.PublishContext = args;

            // Ignora i messaggi del server

            if (args.ClientId == serverId)
                return;

            // Controlla che il topic abbia un'azione corrispondente

            var topic = args.ApplicationMessage.Topic.Split('/');
            var route = _routeTable.MatchPublish(topic);

            if (route is null)
            {
                args.ProcessPublish = false;
                args.Response.ReasonCode = MqttPubAckReasonCode.TopicNameInvalid;
            }
            else
            {
                // Imposta valori di default

                args.CloseConnection = false;
                args.ProcessPublish = true;

                // Crea contesto, crea scope e attiva route

                var context = new MqttContext()
                {
                    PublishEventArgs = args
                };

                await using var scope = _scopeFactory.CreateAsyncScope();
                await using var activator = new RouteActivator(route, topic, context, scope.ServiceProvider);
                await activator.Activate();
            }
        }
        catch (TargetInvocationException e)
        {
            args.ProcessPublish = false;
            args.Response.ReasonCode = MqttPubAckReasonCode.UnspecifiedError;
            _logger.LogCritical(e, "Error in MQTT publish handler for '{topic}': ", args.ApplicationMessage.Topic);
        }
        catch (Exception e)
        {
            args.ProcessPublish = false;
            args.Response.ReasonCode = MqttPubAckReasonCode.UnspecifiedError;
            _logger.LogCritical(e, "Error during MQTT publish handler activation for '{topic}': ", args.ApplicationMessage.Topic);
        }
        finally
        {
            // Resetta contesto

            if (_mqttContextAccessor is not null)
                _mqttContextAccessor.PublishContext = null;
        }
    }

    private async Task InterceptingSubscriptionAsync(InterceptingSubscriptionEventArgs args)
    {
        try
        {
            // Setta contesto

            if (_mqttContextAccessor is not null)
                _mqttContextAccessor.SubscriptionContext = args;

            // Ignora i messaggi del server

            if (args.ClientId == serverId)
                return;

            // Controlla che il topic abbia un'azione corrispondente

            var topic = args.TopicFilter.Topic.Split('/');
            var route = _routeTable.MatchSubscribe(topic);

            if (route is null)
            {
                args.ProcessSubscription = false;
                args.Response.ReasonCode = MqttSubscribeReasonCode.TopicFilterInvalid;
            }
            else
            {
                // Imposta contesto con valori di default

                args.CloseConnection = false;
                args.ProcessSubscription = true;

                // Crea contesto, crea scope e attiva route

                var context = new MqttContext()
                {
                    SubscriptionEventArgs = args
                };

                await using var scope = _scopeFactory.CreateAsyncScope();
                await using var activator = new RouteActivator(route, topic, context, scope.ServiceProvider);
                await activator.Activate();
            }
        }
        catch (TargetInvocationException e)
        {
            args.ProcessSubscription = false;
            args.Response.ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
            _logger.LogCritical(e, "Error in MQTT subscription handler for '{topic}': ", args.TopicFilter.Topic);
        }
        catch (Exception e)
        {
            args.ProcessSubscription = false;
            args.Response.ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
            _logger.LogCritical(e, "Error during MQTT subscription handler activation for '{topic}': ", args.TopicFilter.Topic);
        }
        finally
        {
            // Resetta contesto

            if (_mqttContextAccessor is not null)
                _mqttContextAccessor.SubscriptionContext = null;
        }
    }

    private async Task ValidatingConnectionAsync(ValidatingConnectionEventArgs args)
    {
        try
        {
            if (args.ClientId == serverId)
            {
                // Impedisci accesso se stesso ID del server

                args.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
            }
            else
            {
                // Autentica client

                await using var scope = _scopeFactory.CreateAsyncScope();
                await using var activator = new HandlerActivator<IMqttAuthenticationHandler>(scope.ServiceProvider, _routeTable.AuthenticationHandler!);
                await activator.Handler.AuthenticateAsync(args);
            }
        }
        catch (Exception e)
        {
            args.ReasonCode = MqttConnectReasonCode.UnspecifiedError;
            _logger.LogCritical(e, "Error in MQTT authentication handler for '{clientId}': ", args.ClientId);
        }
    }

    private async Task ClientConnectedAsync(ClientConnectedEventArgs args)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await using var activator = new HandlerActivator<IMqttConnectionHandler>(scope.ServiceProvider, _routeTable.ConnectionHandler!);
            await activator.Handler.ClientConnectedAsync(args);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error in MQTT connection handler for '{clientId}': ", args.ClientId);
        }
    }

    private async Task ClientDisconnectedAsync(ClientDisconnectedEventArgs args)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await using var activator = new HandlerActivator<IMqttConnectionHandler>(scope.ServiceProvider, _routeTable.ConnectionHandler!);
            await activator.Handler.ClientDisconnectedAsync(args);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error in MQTT disconnection handler for '{clientId}': ", args.ClientId);
        }
    }

    public void UseMqttServer(MqttServer server)
    {
        mqttServer = server;

        // Attiva handler pubblicazioni e sottoscrizioni

        server.InterceptingPublishAsync += InterceptingPublishAsync;
        server.InterceptingSubscriptionAsync += InterceptingSubscriptionAsync;

        // Attiva handler autenticazione se necessario

        if (_routeTable.AuthenticationHandler is not null)
        {
            server.ValidatingConnectionAsync += ValidatingConnectionAsync;
        }

        // Attiva handler connessioni se necessario

        if (_routeTable.ConnectionHandler is not null)
        {
            server.ClientConnectedAsync += ClientConnectedAsync;
            server.ClientDisconnectedAsync += ClientDisconnectedAsync;
        }
    }

    public Task Send(MqttApplicationMessage message)
    {
        if (mqttServer is not null)
            return mqttServer.InjectApplicationMessage(new(message)
            {
                SenderClientId = serverId
            });
        else
            throw new InvalidOperationException($"Please call {nameof(UseMqttServer)}() in startup to use this method");
    }
}
