# Server and Client startup

A server is created by calling the constructor:

XmtpServer<TRemoteID> = new XmtpServer<TRemoteID>(
    serverType: "serverType", // name of the controllers that the server uses
    port: 5000, // listener port
    logger: logger, // a logger implementing ILogger interface
    connectionInitializer: initializer, // a connection initializer implementing IConnectionInitializer interface
    authenticator: authenticator, // an authenticator implementing IAuthenticator<TRemoteID> interface
    services: serviceLibrary, // a service library that contains all services required by the endpoints
    certificate: certificate, // the shown up certificate for TLS 
    certificateValidationCallback: validator, // the validator used to validate remote certificate
    useClientAuthentication: true // whether mTLS is required
);

# Discovery process:
1. On creation, the server discovers the controllers with the server type:
    - public, non-abstract class
    - inherited from ServerControllerBase or ClientControllerBase
    - has ServerController or ClientController attribute accordingly to base type.
    - has Route attribute
2. Register their constructor with the most parameters.
3. Check if it is satisfiable with the services in the service library. -> if not: services cannot be resolved for the controller -> exception
4. Precompile constructor:
    - Save constructor reference with all the services required for it
5. Register endpoints: discover endpoints of the controllers:
    - public, non-static method
    - has Endpoint attribute
6. Read endpoint metadata:
    - Request endpoint if has return value:
        - Sync: int, string, ..., object (any custom type)
        - Async: Task<object> (only Task<object>, no other Task<T>)
    - Message endpoint if has no return value:
        - Sync: void
        - Async: Task
7. Compile reflection into delegates
8. Store endpoints in routing table

# Controller and Endpoint discovery

Server controller example:

// assign the server names, the controller belongs to
// if no server name is listed the controller is universal and belongs to all servers
[ServerController("serverType")] 
[Route("route_name")] or [Route] // assign a root name to all endpoints of the controller
public class CustomServerController<T> : ServerControllerBase<T>
{
    ICustomService customService;

    // Constructor with most parameters
    public CustomServerController(ICustomService customService)
    {
        this.customService = customService;
    }

    // endpoint: "route_name/endpoint_name"
    // or "endpoint_name" if Route name is not specified
    [Endpoint("endpoint_name")] 
    void Endpoint(int param1, string param2)
    {
        customService.CustomMethod(param1, param2);
    }
}

# Service management

Services should be created manually before creating the Server or Client instance.
The created services should be added to a ServiceLibrary object that is passed to the server constructor.
The services logged into the Servi

Example:

void LogService()
{
    ICustomService customService = new CustomService();

    ServiceLibrary services = new ServiceLibrary();
    services.AddService<ICustomService>(customService);

    XmtpServer<string> = new XmtpServer(
        services: services
    );
}
