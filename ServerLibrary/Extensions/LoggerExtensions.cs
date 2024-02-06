using System.Data;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Dapr;

namespace ServerLibrary.Extensions
{
    public static class LoggerExtensions
    {
        public static void WriteLogError(this ILogger logger, Exception exception, string? action, string? argumenCall = "")
        {
            if (exception is RpcException)
                logger.LogError("RPC exception in {Action}: {Message}, {argumenCall}", action, exception.Message, argumenCall);
            else if (exception is DaprException)
            {
                logger.LogError("DaprException exception in {Action}: {Message}, {argumenCall}", action, exception.Message, argumenCall);
            }
            else
                logger.LogError("Exception in {Action}: {Message}, {argumenCall}", action, exception.Message, argumenCall);
        }
    }
}
