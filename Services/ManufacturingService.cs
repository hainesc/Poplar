using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using uniffi.stump;
using Poplar.Models;

namespace Poplar.Services;

/// <summary>
/// Service that implements Stump ManufacturingObserver to capture native events 
/// and dispatch them dynamically to WPF view models.
/// Also serves as the global in-memory Hot Data Store for traceability records,
/// keeping up to 100,000 records or records from the last 12 hours.
/// </summary>
public sealed class ManufacturingService : ManufacturingObserver
{
    private readonly BackendService _backend;
    private bool _isSubscribed;

    // Central hot data cache matching Bear's 12h/100k React Context retention policy
    private readonly object _lock = new();
    private readonly Queue<TraceabilityRecord> _hotTraceRecords = new();

    public ManufacturingService(BackendService backend)
    {
        _backend = backend;
    }

    /// <summary>
    /// Fetches all currently held hot data traceability records in a thread-safe manner.
    /// </summary>
    public IReadOnlyList<TraceabilityRecord> GetHotTraceRecords()
    {
        lock (_lock)
        {
            return _hotTraceRecords.ToList();
        }
    }

    /// <summary>
    /// Thread-safe insertion and pruning of incoming trace records.
    /// </summary>
    public void AddTraceRecord(TraceabilityRecord record)
    {
        lock (_lock)
        {
            _hotTraceRecords.Enqueue(record);
            PruneHotRecords();
        }
    }

    private void PruneHotRecords()
    {
        var cutoff = DateTime.Now.AddHours(-12);

        // 1. Evict records older than 12 hours from the head (extremely fast O(1) checks)
        while (_hotTraceRecords.Count > 0)
        {
            var oldest = _hotTraceRecords.Peek();
            if (DateTime.TryParse(oldest.CreatedAt, out var dt))
            {
                if (dt < cutoff)
                {
                    _hotTraceRecords.Dequeue();
                    continue;
                }
            }
            // Once we hit a fresh record, we can safely stop because subsequent items are newer
            break;
        }

        // 2. Cap maximum quantity at 100,000 entries (extremely fast O(1) dequeue)
        while (_hotTraceRecords.Count > 100000)
        {
            _hotTraceRecords.Dequeue();
        }
    }

    /// <summary>
    /// Starts the FFI observer registration with the Rust backend.
    /// </summary>
    public async Task StartSubscriptionAsync()
    {
        if (_isSubscribed) return;

        try
        {
            if (!_backend.IsInitialized)
            {
                await _backend.InitializeAsync();
            }

            await _backend.ManufacturingVm.Subscribe(this);
            _isSubscribed = true;
            System.Diagnostics.Debug.WriteLine("[ManufacturingService] Successfully subscribed to Manufacturing events.");
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ManufacturingService] Subscription failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Stump callback handler.
    /// </summary>
    public void OnEvent(ManufacturingEvent @event)
    {
        // Intercept traceability records and append them to our hot store
        if (@event is ManufacturingEvent.TraceabilityRecorded trace)
        {
            var newRecord = new TraceabilityRecord(
                trace.productId,
                trace.workOrderId,
                trace.processId,
                trace.result,
                trace.data,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            );

            AddTraceRecord(newRecord);
        }

        // Broadcast the event to any active subscribers on the messenger pipeline
        WeakReferenceMessenger.Default.Send(new ManufacturingEventMessage(@event));
    }
}
