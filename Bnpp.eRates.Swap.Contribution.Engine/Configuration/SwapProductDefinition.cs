namespace Bnpp.eRates.Swap.Contribution.Engine.Configuration;

/// <summary>
/// Centralised product configuration. Binds to the "Product" section of appsettings.json.
/// This single config object replaces all the scattered constants, options classes, and
/// hardcoded strings that were previously duplicated across every product's Host and Library.
/// </summary>
public class SwapProductDefinition
{
    public const string SectionName = "Product";

    /// <summary>Product identifier, e.g. "SwapLatam", "SwapInflation".</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Environment variable name used to resolve the running instance.
    /// e.g. "ERATESWEB_LDN_DEV_HOST_SWAP_LATAM"</summary>
    public string InstanceEnvVar { get; set; } = string.Empty;

    /// <summary>Endpoint key used with AppConfig.GetEndpoint().
    /// e.g. "SwapLatam" → EndpointConstants.SwapLatam</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Endpoint type string for connection manager.
    /// e.g. "SwapLatamContribution"</summary>
    public string EndpointType { get; set; } = string.Empty;

    /// <summary>Default tier for new client subscriptions.</summary>
    public string DefaultTier { get; set; } = string.Empty;

    /// <summary>Throttle delay in seconds for contribution updates.</summary>
    public double ThrottleDelay { get; set; } = 1.0;

    /// <summary>Authorization policy configuration.</summary>
    public SwapPolicyConfig Policies { get; set; } = new();

    /// <summary>Enabled capabilities — controls which hub methods are active.
    /// See <see cref="SwapContributionCapability"/> for valid values.</summary>
    public List<string> Capabilities { get; set; } = new();

    /// <summary>Feed connection configuration.</summary>
    public SwapFeedConfig Feeds { get; set; } = new();

    /// <summary>SignalR event names sent to clients.</summary>
    public SwapSignalRConfig SignalR { get; set; } = new();

    public bool HasCapability(string capability) =>
        Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);
}

public class SwapPolicyConfig
{
    public string Read { get; set; } = string.Empty;
    public string Write { get; set; } = string.Empty;
}

public class SwapFeedConfig
{
    /// <summary>One or more Orion contribution feed connections.
    /// Latam has 1, Inflation has 4 — purely config-driven.</summary>
    public List<SwapFeedConnectionConfig> Contributions { get; set; } = new();

    /// <summary>Instrument feed connection.</summary>
    public SwapInstrumentConnectionConfig Instruments { get; set; } = new();
}

public class SwapFeedConnectionConfig
{
    /// <summary>Key used with AppConfig.GetConnection().
    /// e.g. "SwapLatamContributions", "SwapInflationContributionsTweb"</summary>
    public string ConnectionKey { get; set; } = string.Empty;
}

public class SwapInstrumentConnectionConfig
{
    /// <summary>Key used with AppConfig.GetConnection() for the instrument feed.
    /// e.g. "SwapLatamInstruments"</summary>
    public string ConnectionKey { get; set; } = string.Empty;
}

public class SwapSignalRConfig
{
    public string EventContributionUpdate { get; set; } = "onContributionUpdate";
    public string EventNotification { get; set; } = "onNotification";
}
