using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Storage;

namespace Spx.Silo.Infrastructure;

internal sealed partial class FaultTolerantJsonGrainStorageSerializer(
    ILogger<FaultTolerantJsonGrainStorageSerializer> logger
) : IGrainStorageSerializer
{
    public BinaryData Serialize<T>(T input) =>
        BinaryData.FromString(JsonConvert.SerializeObject(input));

    public T Deserialize<T>(BinaryData input)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(input.ToString())!;
        }
        catch (JsonException exception)
        {
            LogDeserializationFailed(exception, typeof(T).Name);
            throw new InvalidOperationException(
                $"Grain state '{typeof(T).Name}' could not be deserialized from storage.",
                exception
            );
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to deserialize grain state of type '{TypeName}' from storage."
    )]
    private partial void LogDeserializationFailed(Exception exception, string typeName);
}
