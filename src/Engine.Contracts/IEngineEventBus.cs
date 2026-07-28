namespace Engine.Contracts;

/// <summary>
/// Interface for an event bus that allows publishing and subscribing to engine events.
/// </summary>
public interface IEngineEventBus
{
    IObservable<EngineEvent> Events { get; }

    void Publish(EngineEvent engineEvent);
}

/// <summary>
/// Event representing a significant occurrence in the processing engine, including its type, message, and timestamp.
/// </summary>
/// <param name="Type"></param>
/// <param name="Message"></param>
/// <param name="Timestamp"></param>
public sealed record EngineEvent(
    EngineEventType Type, 
    string Message,
    DateTimeOffset Timestamp);


/// <summary>
/// Enumeration of possible event types that can be published by the processing engine, indicating the nature of the event.
/// </summary>
public enum EngineEventType
{
    Info,
    Started,
    Progress,
    Completed,
    Cancelled,
    Warning,
    Error
}


