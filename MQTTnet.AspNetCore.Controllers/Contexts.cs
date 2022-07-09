using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Server;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace MQTTnet.AspNetCore.Controllers;

public class AuthenticationContext
{
    public string ClientId { get; }
    public string Endpoint { get; }
    public bool IsSecureConnection { get; }
    public X509Certificate2 ClientCertificate { get; }
    public MqttProtocolVersion ProtocolVersion { get; }
    public string Username { get; }
    public byte[] RawPassword { get; }
    public string Password { get; }

    public AuthenticationContext(ValidatingConnectionEventArgs context)
    {
        ClientId = context.ClientId;
        Endpoint = context.Endpoint;
        IsSecureConnection = context.IsSecureConnection;
        ClientCertificate = context.ClientCertificate;
        ProtocolVersion = context.ProtocolVersion;
        Username = context.Username;
        RawPassword = context.RawPassword;
        Password = context.Password;
    }
}

public class PublishContext
{
    public CancellationToken RequestAborted { get; }
    public string ClientId { get; }
    public MqttApplicationMessage ApplicationMessage { get; }

    public PublishContext(InterceptingPublishEventArgs context)
    {
        RequestAborted = context.CancellationToken;
        ClientId = context.ClientId;
        ApplicationMessage = context.ApplicationMessage;
    }
}

public class SubscriptionContext
{
    public CancellationToken RequestAborted { get; }
    public string ClientId { get; }
    public MqttTopicFilter TopicFilter { get; }

    public SubscriptionContext(InterceptingSubscriptionEventArgs context)
    {
        RequestAborted = context.CancellationToken;
        ClientId = context.ClientId;
        TopicFilter = context.TopicFilter;
    }
}
