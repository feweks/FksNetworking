namespace FksNetworking.Events;

public class EventsManager
{
    private Dictionary<string, List<Action<EventArgs>>> events = new Dictionary<string, List<Action<EventArgs>>>();

    public void Add(string name, Action<EventArgs> callback)
    {
        if (!events.ContainsKey(name)) events.Add(name, new List<Action<EventArgs>>());

        events[name].Add(callback);
    }

    public void Remove(string name)
    {
        if (!events.ContainsKey(name)) return;

        events.Remove(name);
    }

    public void Dispatch(string name, EventArgs args)
    {
        if (!events.ContainsKey(name)) return;

        foreach (var ev in events[name])
        {
            ev.Invoke(args);
        }
    }
}
