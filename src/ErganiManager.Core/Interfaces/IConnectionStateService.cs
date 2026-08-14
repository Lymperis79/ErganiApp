using ErganiManager.Core.Models;
using ErganiManager.Data;

namespace ErganiManager.Core.Interfaces;

public interface IConnectionStateService
{
    /// <summary>Checks for connection.json and tests connectivity. Call once at startup
    /// and again whenever the user wants to retry after a degraded state.</summary>
    Task<AppConnectionState> EvaluateAsync();

    AppConnectionState CurrentState { get; }

    DbConfig? LoadConfig();

    void SaveConfig(DbConfig config);

    bool ConfigExists();
}
