using System.Collections.Concurrent;
using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Swap.Contribution.Engine.Feeds;
using Bnpp.SmartBase.AppBase.Contracts;
using Bnpp.SmartBase.Model.Common;

namespace Bnpp.eRates.Swap.Contribution.Engine.Providers;

/// <summary>
/// Generic instrument provider. Replaces SwapLatamInstrumentProvider, SwapInflationInstrumentProvider, etc.
/// All per-product providers were identical: maintain a ConcurrentDictionary, subscribe via the feed.
/// </summary>
internal class GenericSwapInstrumentProvider<TInstrument> : IInstrumentProvider<TInstrument>
    where TInstrument : class, ISwapInstrument, IKey<string>, IIsDeleted
{
    private readonly ConcurrentDictionary<string, TInstrument> _allInstruments = new();

    public GenericSwapInstrumentProvider(IDispatcher dispatcher, ISwapInstrumentFeed<TInstrument> instrumentFeed)
    {
        var modelCollection = new ModelCollection<string, TInstrument>(instrumentFeed.SubscribeToInstruments);

        modelCollection.Subscribe(dispatcher,
            initialList =>
            {
                foreach (var instrument in initialList)
                {
                    _allInstruments.AddOrUpdate(instrument.Key, instrument, (_, _) => instrument);
                }
            },
            updated => _allInstruments.AddOrUpdate(updated.Key, updated, (_, _) => updated),
            removedKey => _allInstruments.TryRemove(removedKey, out _),
            () => _allInstruments.Clear());
    }

    public IEnumerable<TInstrument> GetAllInstruments() => _allInstruments.Values;

    public bool TryGetInstrument(string instrumentId, out TInstrument instrument) =>
        _allInstruments.TryGetValue(instrumentId, out instrument!);
}
