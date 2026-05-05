namespace BaristaApp.Api.Models;

public sealed record AylaDevice(
    string Dsn,
    string ProductName,
    string OemModel,
    string ConnectionStatus,
    string? SwVersion);
