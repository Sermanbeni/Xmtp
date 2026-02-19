using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class ServerConfig
{
    public string ServerType { get; set; }
    public int Port { get; set; }
    public int TokenSize { get; set; }
    public int TimeoutDuration { get; set; }

    public ServerConfig()
    {
        ServerType = string.Empty;
        Port = 0;
        TokenSize = 0;
        TimeoutDuration = 0;
    }

    public ServerConfig(string serverType, int port, int tokenSize, int timeoutDuration)
    {
        ServerType = serverType;
        Port = port;
        TokenSize = tokenSize;
        TimeoutDuration = timeoutDuration;
    }

    public static ServerConfig Default = new ServerConfig("server", 5000, 32, 15000);

    public static ServerConfig LoadConfig(string path)
    {
        ServerConfig? config;
        string conf;

        if (!File.Exists(path))
        {
            throw new FileNotFoundException();
        }

        conf = File.ReadAllText(path);
        config = JsonSerializer.Deserialize<ServerConfig>(conf);

        if (config == null)
        {
            throw new FormatException();
        }

        return config;
    }

    public static ServerConfig LoadOrCreateConfig(string path)
    {
        ServerConfig? config;
        string conf;

        if (!File.Exists(path))
        {
            config = Default;
            conf = JsonSerializer.Serialize(config);
            File.WriteAllText(path, conf);
            return config;
        }

        conf = File.ReadAllText(path);
        config = JsonSerializer.Deserialize<ServerConfig>(conf);

        if (config == null)
        {
            config = Default;
            conf = JsonSerializer.Serialize(config);
            File.WriteAllText(path, conf);
            return config;
        }

        return config;
    }

    public static void SaveConfig(string path, ServerConfig config)
    {
        string conf = JsonSerializer.Serialize(config);
        File.WriteAllText(path, conf);
    }
}

