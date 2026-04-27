using System.Text.Json;

namespace WhatsAppToDB
{

    public class UserService
    {
        public UserService() { }

        public static (bool isSuccess, UserSession session) ValidateLogin(string username, string password)
        {
            var users = JsonSerializer.Deserialize<List<dynamic>>(File.ReadAllText("users.json"));
            // Simple lookup (in production, use password hashing!)
            var user = users.FirstOrDefault(u => u.GetProperty("Username").GetString() == username
                                             && u.GetProperty("Password").GetString() == password);

            if (user.ValueKind == JsonValueKind.Undefined) return (false, null);

            var token = Guid.NewGuid().ToString(); // Simple token generation
            var session = new UserSession(token,
                user.GetProperty("Username").GetString(),
                user.GetProperty("Role").GetString(),
                user.GetProperty("InternalUserId").GetString(),
                user.GetProperty("SessionContextKey").GetString());

            // Save session to tokens.json
            var sessions = File.Exists("tokens.json")
                ? JsonSerializer.Deserialize<List<UserSession>>(File.ReadAllText("tokens.json"))
                : new List<UserSession>();

            sessions.Add(session);
            File.WriteAllText("tokens.json", JsonSerializer.Serialize(sessions));
            return (true, session);
        }

        private static (bool isSuccess, Abstractions.IdentityContext? identity) GetIdentity(UserSession session)
        {
            if (session == null) return (false, null);
            Abstractions.IdentityContext ic = new Abstractions.IdentityContext();
            ic.InternalUserId = session.InternalUserId;
            ic.Role = session.Role;
            ic.SessionContextKey = session.SessionContextKey;
            ic.WhatsAppNumber = session.Username;            

            return (true, ic);
        }

        public static (bool isSuccess, Abstractions.IdentityContext? identity) ValidateUserName(string username)
        {
            var sessions = JsonSerializer.Deserialize<List<UserSession>>(File.ReadAllText("tokens.json"));
            var session = sessions.FirstOrDefault(s => s.Username == username);
            return GetIdentity(session);
        }

        public static (bool isSuccess, Abstractions.IdentityContext? identity) ValidateToken(string token)
        {
            var sessions = JsonSerializer.Deserialize<List<UserSession>>(File.ReadAllText("tokens.json"));
            var session = sessions.FirstOrDefault(s => s.Token == token);
            return GetIdentity(session);
        }
    }

        public record LoginRequest(string Username, string Password);
    public record AskRequest(string Question, long? SessionId);
    public record UserSession(string Token, string Username, string Role, string InternalUserId, string SessionContextKey);
}
