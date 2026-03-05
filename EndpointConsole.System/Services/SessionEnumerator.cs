using System.ComponentModel;
using System.Runtime.InteropServices;
using EndpointConsole.Core.Interfaces;
using EndpointConsole.Core.Models;
using EndpointConsole.WindowsSystem.Interop;
using Microsoft.Extensions.Logging;

namespace EndpointConsole.WindowsSystem.Services;

public sealed class SessionEnumerator(ILogger<SessionEnumerator> logger) : ISessionEnumerator
{
    private readonly ILogger<SessionEnumerator> _logger = logger;

    public Task<IReadOnlyList<UserSessionInfo>> EnumerateSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!WtsNativeMethods.WTSEnumerateSessions(
                WtsNativeMethods.CurrentServerHandle,
                0,
                1,
                out var sessionInfoPointer,
                out var sessionCount))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "WTSEnumerateSessions failed.");
        }

        var sessions = new List<UserSessionInfo>(sessionCount);
        try
        {
            var structSize = Marshal.SizeOf<WtsNativeMethods.WtsSessionInfo>();
            for (var index = 0; index < sessionCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentPointer = IntPtr.Add(sessionInfoPointer, index * structSize);
                var sessionInfo = Marshal.PtrToStructure<WtsNativeMethods.WtsSessionInfo>(currentPointer);

                var sessionName = Marshal.PtrToStringUni(sessionInfo.WinStationName) ?? string.Empty;
                var userName = QuerySessionString(sessionInfo.SessionId, WtsNativeMethods.WtsInfoClass.WTSUserName);
                var domain = QuerySessionString(sessionInfo.SessionId, WtsNativeMethods.WtsInfoClass.WTSDomainName);

                sessions.Add(new UserSessionInfo
                {
                    SessionId = sessionInfo.SessionId,
                    SessionName = sessionName,
                    UserName = userName,
                    Domain = domain,
                    State = sessionInfo.State.ToString()
                });
            }
        }
        finally
        {
            WtsNativeMethods.WTSFreeMemory(sessionInfoPointer);
        }

        var orderedSessions = sessions
            .OrderBy(session => session.SessionId)
            .ToList();

        _logger.LogInformation("Enumerated {Count} interactive sessions.", orderedSessions.Count);
        return Task.FromResult<IReadOnlyList<UserSessionInfo>>(orderedSessions);
    }

    private string QuerySessionString(int sessionId, WtsNativeMethods.WtsInfoClass infoClass)
    {
        if (!WtsNativeMethods.WTSQuerySessionInformation(
                WtsNativeMethods.CurrentServerHandle,
                sessionId,
                infoClass,
                out var buffer,
                out var bytesReturned))
        {
            return string.Empty;
        }

        try
        {
            if (buffer == IntPtr.Zero || bytesReturned <= 1)
            {
                return string.Empty;
            }

            return Marshal.PtrToStringUni(buffer)?.Trim() ?? string.Empty;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                WtsNativeMethods.WTSFreeMemory(buffer);
            }
        }
    }
}
