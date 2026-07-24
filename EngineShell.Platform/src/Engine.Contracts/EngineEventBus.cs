using System.Reactive.Subjects;

namespace Engine.Contracts;

public sealed class EngineEventBus : IEngineEventBus, IDisposable
{
    private readonly Subject<EngineEvent> _events = new();

    public IObservable<EngineEvent> Events => _events;

    public void Publish(EngineEvent engineEvent)
    {
        ArgumentNullException.ThrowIfNull(engineEvent);
        _events.OnNext(engineEvent);
    }

    public void Dispose()
    {
        _events.Dispose();
    }
}