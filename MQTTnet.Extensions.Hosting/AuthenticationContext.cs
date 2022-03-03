using MQTTnet.Formatter;
using MQTTnet.Server;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Extensions.Hosting;

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

    public AuthenticationContext(MqttConnectionValidatorContext context)
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
