using System.Collections.Concurrent;

namespace Xmtp
{
    public class Authenticator<T> : IAuthenticator<T> where T : notnull
    {
        ConcurrentDictionary<string, T> storedTokens = new();
        ConcurrentDictionary<T, string> reverseTokens = new();

        string GetToken(byte[] token)
        {
            return Convert.ToBase64String(token);
        }

        public bool RegisterToken(byte[] token, T remoteID)
        {
            string t = GetToken(token);
            if (storedTokens.TryAdd(t, remoteID))
            {
                return reverseTokens.TryAdd(remoteID, t);
            }
            return false;
        }

        public bool RemoveToken(byte[] token)
        {
            string t = GetToken(token);
            if (storedTokens.TryRemove(t, out T? remoteID))
            {
                return reverseTokens.TryRemove(remoteID!, out _);
            }
            return false;
        }

        public bool RemoveRemote(T remoteID)
        {
            if (reverseTokens.TryRemove(remoteID, out string? t))
            {
                return storedTokens.TryRemove(t!, out _);
            }
            return false;
        }

        public bool Authenticate(byte[] token, out T? ID)
        {
            string t = GetToken(token);
            return storedTokens.TryGetValue(t, out ID);
        }
    }
}
