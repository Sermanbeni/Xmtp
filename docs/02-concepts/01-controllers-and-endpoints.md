# Controllers and Endpoints

Endpoints are the methods on a controller that can be called remotely.
Controllers are container classes that contain endpoints.

Controllers and endpoints are discovered and precompiled automatically during server build time.

## Controllers:
- To define a controller:
    1. The class must extend one of the Controller Base classes:
        - ServerControllerBase (if server controller)
        - ClientControllerBase (if client controller)
    2. The class must also have 2 attributes:
        - Route (defines the endpoint path root to the endpoint)
        - ServerController (if server)
        - ClientController (if client)
    3. The class must be public

- A controller of every type is created to all connections when connecting.

## Endpoints:
- To define an endpoint:
    - The method must be a public method in a Controller.
    - The method must have 1 attribute:
        - Endpoint (defines the endpoint route ending to the endpoint)
        - Final endpoint path = RoutePath/EndpointPath

### Endpoint Types:
1. Request:
    - Has a return value. The endpoint is invoked and the return value gets returned as a response.
    - To define a Request endpoint, the method must have a return value:
        - For synchronous endpoints: returns any custom type is accepted (int, string, custom serializable classes, object)
        - For asynchronous endpoints: returns `Task<object>` (strictly `Task<object>`, no `Task<T>` allowed)

2. Message:
    - Has no return value. The endpoint is invoked and nothing is returned.
    - To define a Message endpoint, the method must have no return value:
        - For synchronous endpoints: returns void
        - For asynchronous endpoints: returns Task
