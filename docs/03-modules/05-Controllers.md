# Controllers

## Controller Base
Controller Base classes are the base classes to create controllers.

1. `ClientControllerBase<T>`:
    - Has a public `XmtpClient<T>` client field: The client on which the controller is registered.

2. `ServerControllerBase<T>`:
    - Has a public `XmtpServer<T>` server field: The server to which the controller is registered.
    - Has a public `T` Remote ID field: The ID of the remote to which the controller belongs to.
        - It can be used for authenticating the connected user per message.

## Controller Attributes

Controller attributes are attributes to the controllers. Client and Server controllers work identical way.

1. `ServerControllerAttribute()`
    - Creates a universal controller that is logged for all servers.
2. `ServerControllerAttribute(params string[] configurations)`
    - Creates a controller that is logged for all servers with one of the configurations.
3. `ClientControllerAttribute()`
    - Creates a universal controller that is logged for all cliets.
4. `ClientControllerAttribute(params string[] configurations)`
    - Creates a controller that is logged for all clients with one of the configurations.

