using Bnpp.SmartBase.AppBase.Contracts;

namespace Bnpp.eRates.Swap.Contribution.Engine.Feeds;

/// <summary>
/// Generic instrument feed interface. Replaces the per-product interfaces
/// (ISwapLatamInstrumentFeed, ISwapInflationInstrumentFeed, etc.)
/// which were identical apart from the instrument type parameter.
/// </summary>
public interface ISwapInstrumentFeed<out TInstrument>
{
    ISubscription SubscribeToInstruments(
        Action<TInstrument> onAdd,
        Action<TInstrument>? onUpdate = null,
        Action<string>? onRemove = null,
        Action? onRemoveAll = null);
}
