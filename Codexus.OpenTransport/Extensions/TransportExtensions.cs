using System.Net.NetworkInformation;
using Codexus.OpenSDK.Entities;

namespace Codexus.OpenTransport.Extensions;

public static class TransportExtensions
{
    private static bool IsPortFree(int port, bool reuseTimeWait = true)
    {
        var used = GetUsedPorts(reuseTimeWait);
        return !used.Contains(port);
    }

    private static HashSet<int> GetUsedPorts(bool reuseTimeWait = true)
    {
        var ipProps = IPGlobalProperties.GetIPGlobalProperties();
        var tcpListeners = ipProps.GetActiveTcpListeners().Select(e => e.Port);
        var udpListeners = ipProps.GetActiveUdpListeners().Select(e => e.Port);

        var tcpConnections = ipProps.GetActiveTcpConnections();

        var filteredConnections = reuseTimeWait
            ? tcpConnections.Where(c =>
                c.State != TcpState.TimeWait &&
                c.State != TcpState.CloseWait)
            : tcpConnections;

        var tcpPorts = filteredConnections.Select(c => c.LocalEndPoint.Port);

        return
        [
            ..tcpListeners
                .Concat(udpListeners)
                .Concat(tcpPorts)
        ];
    }

    extension(int port)
    {
        public Result<int> FindFreePort(int maxAttempts, bool reuseTimeWait = true)
        {
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var candidatePort = port + attempt;

                if (IsPortFree(candidatePort, reuseTimeWait)) return Result<int>.Success(candidatePort);
            }

            return Result<int>.Failure(
                $"No available port found in range {port}-{port + maxAttempts - 1}");
        }
    }
}