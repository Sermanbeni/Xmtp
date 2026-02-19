using System.Collections.ObjectModel;

public class ServiceLibrary
{
    Dictionary<Type, object> services = new();

    public void AddService<T>(object service) where T : class
    {
        if (service is not T)
        {
            throw new InvalidCastException();
        }
        services.TryAdd(typeof(T), service);
    }

    public ReadOnlyDictionary<Type, object> RegisteredServices => services.AsReadOnly();
}
