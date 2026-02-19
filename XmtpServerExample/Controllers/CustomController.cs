using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xmtp;

[ServerController("server")] // set the server names for which server the endpoint needs to be configured. (serverType)
                             // If not set, the controller goes to all servers
[Route("custom")] // set the root of the endpoints
public class CustomController<T> : ServerControllerBase<T>
{
    ICustomService customService;

    public CustomController(ICustomService customService)
    {
        this.customService = customService;
    }

    [Endpoint("hello")] // endpoint: custom/hello
    public void Hello() // returns void: standard endpoint
    {
        customService.SayHello();
    }

    [Endpoint("get_hello")] // endpoint: custom/get_hello
    public string ReturnHello() // returns string: request endpoint
    {
        return "Hello world";
    }

    [Endpoint("hello_async")] // endpoint: custom/hello_async
    public Task HelloAsync(int n) // returns Task: standard endpoint
    {
        for (int i = 0; i < n; i++)
        {
            customService.SayHello();
        }
        return Task.CompletedTask;
    }

    [Endpoint("get_hello_async")] // endpoint: custom/get_hello_async
    public Task<object> ReturnHelloAsync() // returns Task<object>: request endpoint
    {
        return Task.FromResult((object)"Hello World");
    }
}
