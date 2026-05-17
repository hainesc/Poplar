using uniffi.stump;

namespace Poplar.Models;

/// <summary>
/// Mapped traceability record representation matching the database structure.
/// </summary>
public record TraceabilityRecord(
    int ProductId,
    int WorkOrderId,
    string ProcessId,
    string Result,
    string Data,
    string CreatedAt
);

/// <summary>
/// Weak messenger notification wrapped event from the Rust interop.
/// </summary>
public record ManufacturingEventMessage(ManufacturingEvent Event);
