using System.Text.Json.Serialization;

namespace NimbusStation.Providers.Azure.Auth;

/// <summary>
/// Represents the JSON output from 'az account show'.
/// </summary>
internal sealed class AzureAccountInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }

    [JsonPropertyName("tenantDisplayName")]
    public string? TenantDisplayName { get; set; }

    [JsonPropertyName("user")]
    public AzureUserInfo? User { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }
}

/// <summary>
/// Represents the user info within an Azure account.
/// </summary>
internal sealed class AzureUserInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
