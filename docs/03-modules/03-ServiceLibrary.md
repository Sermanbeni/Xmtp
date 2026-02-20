# Service Library
A `ServiceLibrary` is a library that contains registered services for the controllers.

1. `void AddService<T>(object service)`
    - Registers a T type service.

Example:

```C#
IService service = new Service();
ServiceLibrary serviceLibrary = new ServiceLibrary();
serviceLibrary.AddService<IService>(service);
```