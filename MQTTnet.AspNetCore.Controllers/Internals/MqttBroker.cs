using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Reflection;

namespace MQTTnet.AspNetCore.Controllers.Internals;

sealed class MqttBroker(ILogger<MqttBroker> logger, RouteTable routeTable, IServiceScopeFactory scopeFactory, IMqttContextAccessor? mqttContextAccessor = null) : IMqttBroker
{
    private MqttServer? mqttServer;

    private async Task InterceptingPublishAsync(InterceptingPublishEventArgs args)
    {
        try
        {
            // Setta contesto

            if (mqttContextAccessor is not null)
                mqttContextAccessor.PublishContext = args;

            // Controlla che il topic abbia un'azione corrispondente

            var topic = args.ApplicationMessage.Topic.Split('/');
            var route = routeTable.MatchPublish(topic);

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

                await using var scope = scopeFactory.CreateAsyncScope();
                await using var activator = new RouteActivator(route, topic, context, scope.ServiceProvider);
                await activator.ActivateAsync();
            }
        }
        catch (TargetInvocationException e)
        {
            args.ProcessPublish = false;
            args.Response.ReasonCode = MqttPubAckReasonCode.UnspecifiedError;
            logger.LogCritical(e, "Error in MQTT publish handler for '{topic}': ", args.ApplicationMessage.Topic);
        }
        catch (Exception e)
        {
            args.ProcessPublish = false;
            args.Response.ReasonCode = MqttPubAckReasonCode.UnspecifiedError;
            logger.LogCritical(e, "Error during MQTT publish handler activation for '{topic}': ", args.ApplicationMessage.Topic);
        }
        finally
        {
            // Resetta contesto

            if (mqttContextAccessor is not null)
                mqttContextAccessor.PublishContext = null;
        }
    }

    private async Task InterceptingSubscriptionAsync(InterceptingSubscriptionEventArgs args)
    {
        try
        {
            // Setta contesto

            if (mqttContextAccessor is not null)
                mqttContextAccessor.SubscriptionContext = args;

            // Controlla che il topic abbia un'azione corrispondente

            var topic = args.TopicFilter.Topic.Split('/');
            var route = routeTable.MatchSubscribe(topic);

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

                await using var scope = scopeFactory.CreateAsyncScope();
                await using var activator = new RouteActivator(route, topic, context, scope.ServiceProvider);
                await activator.ActivateAsync();
            }
        }
        catch (TargetInvocationException e)
        {
            args.ProcessSubscription = false;
            args.Response.ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
            logger.LogCritical(e, "Error in MQTT subscription handler for '{topic}': ", args.TopicFilter.Topic);
        }
        catch (Exception e)
        {
            args.ProcessSubscription = false;
            args.Response.ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
            logger.LogCritical(e, "Error during MQTT subscription handler activation for '{topic}': ", args.TopicFilter.Topic);
        }
        finally
        {
            // Resetta contesto

            if (mqttContextAccessor is not null)
                mqttContextAccessor.SubscriptionContext = null;
        }
    }

    private async Task ValidatingConnectionAsync(ValidatingConnectionEventArgs args)
    {
        try
        {
            // Autentica client

            await using var scope = scopeFactory.CreateAsyncScope();
            await using var activator = new HandlerActivator<IMqttAuthenticationHandler>(scope.ServiceProvider, routeTable.AuthenticationHandler!);
            await activator.Handler.AuthenticateAsync(args);
        }
        catch (Exception e)
        {
            args.ReasonCode = MqttConnectReasonCode.UnspecifiedError;
            logger.LogCritical(e, "Error in MQTT authentication handler for '{clientId}': ", args.ClientId);
        }
    }

    private async Task ClientConnectedAsync(ClientConnectedEventArgs args)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await using var activator = new HandlerActivator<IMqttConnectionHandler>(scope.ServiceProvider, routeTable.ConnectionHandler!);
            await activator.Handler.ClientConnectedAsync(args);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Error in MQTT connection handler for '{clientId}': ", args.ClientId);
        }
    }

    private async Task ClientDisconnectedAsync(ClientDisconnectedEventArgs args)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await using var activator = new HandlerActivator<IMqttConnectionHandler>(scope.ServiceProvider, routeTable.ConnectionHandler!);
            await activator.Handler.ClientDisconnectedAsync(args);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Error in MQTT disconnection handler for '{clientId}': ", args.ClientId);
        }
    }

    private async Task LoadingRetainedMessageAsync(LoadingRetainedMessagesEventArgs args)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await using var activator = new HandlerActivator<IMqttRetentionHandler>(scope.ServiceProvider, routeTable.RetentionHandler!);
            await activator.Handler.LoadingRetainedMessagesAsync(args);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Error in MQTT retention handler: ");
        }
    }

    private async Task RetainedMessageChangedAsync(RetainedMessageChangedEventArgs args)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await using var activator = new HandlerActivator<IMqttRetentionHandler>(scope.ServiceProvider, routeTable.RetentionHandler!);
            await activator.Handler.RetainedMessageChangedAsync(args);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Error in MQTT retention handler: ");
        }
    }

    private async Task RetainedMessagesClearedAsync(EventArgs args)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await using var activator = new HandlerActivator<IMqttRetentionHandler>(scope.ServiceProvider, routeTable.RetentionHandler!);
            await activator.Handler.RetainedMessagesClearedAsync(args);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Error in MQTT retention handler: ");
        }
    }

    public void UseMqttServer(MqttServer server)
    {
        mqttServer = server;

        // Attiva handler pubblicazioni e sottoscrizioni

        server.InterceptingPublishAsync += InterceptingPublishAsync;
        server.InterceptingSubscriptionAsync += InterceptingSubscriptionAsync;

        // Attiva handler autenticazione se necessario

        if (routeTable.AuthenticationHandler is not null)
        {
            server.ValidatingConnectionAsync += ValidatingConnectionAsync;
        }

        // Attiva handler connessioni se necessario

        if (routeTable.ConnectionHandler is not null)
        {
            server.ClientConnectedAsync += ClientConnectedAsync;
            server.ClientDisconnectedAsync += ClientDisconnectedAsync;
        }

        // Attiva handler retention se necessario

        if (routeTable.RetentionHandler is not null)
        {
            server.LoadingRetainedMessageAsync += LoadingRetainedMessageAsync;
            server.RetainedMessageChangedAsync += RetainedMessageChangedAsync;
            server.RetainedMessagesClearedAsync += RetainedMessagesClearedAsync;
        }
    }

    public Task SendMessageAsync(string clientId, MqttApplicationMessage message)
    {
        if (mqttServer is not null)
            return mqttServer.InjectApplicationMessage(new(message)
            {
                SenderClientId = clientId
            });
        else
            throw new InvalidOperationException($"Please call {nameof(UseMqttServer)}() in startup to use this method");
    }

    public Task ClearRetainedMessagesAsync()
    {
        if (mqttServer is not null)
            return mqttServer.DeleteRetainedMessagesAsync();
        else
            throw new InvalidOperationException($"Please call {nameof(UseMqttServer)}() in startup to use this method");
    }
}
