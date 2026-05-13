using System.Text.Json;

namespace WhatsAppToDB.Services
{

    public class UserService
    {

        static string tokenfilename = "token.json";

        public UserService() { }

        private static List<UserSession> GetUserSessions()
        {
            var sessions = File.Exists(tokenfilename)
                ? JsonSerializer.Deserialize<List<UserSession>>(File.ReadAllText(tokenfilename))
                : new List<UserSession>();
            return sessions;
        }



        public static (bool isSuccess, UserSession session) ValidateLogin(JsonConfigService jsonConfigService, string username, string password)
        {
            //var users = JsonSerializer.Deserialize<List<dynamic>>(File.ReadAllText("users.json"));
            var users = jsonConfigService.GetUsers();
            var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user == null) {
                return (false, null);
            }

            var token = Guid.NewGuid().ToString(); // Simple token generation
            var session = new UserSession(token, user.Username, user.Role, user.InternalUserId, user.SessionContextKey, user.DefaultDatabase);

            // Save session to tokens.json
            var sessions = GetUserSessions();

            sessions.Add(session);
            File.WriteAllText(tokenfilename, JsonSerializer.Serialize(sessions));
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
            var sessions = GetUserSessions();
            var session = sessions.FirstOrDefault(s => s.Username == username);
            return GetIdentity(session);
        }

        public static (bool isSuccess, Abstractions.IdentityContext? identity) ValidateToken(string token)
        {
            var sessions = GetUserSessions();
            var session = sessions.FirstOrDefault(s => s.Token == token);
            return GetIdentity(session);
        }
    }

    public record LoginRequest(string Username, string Password);
    public record AskRequest(string Question, long? SessionId);
    public record UserSession(string Token, string Username, string Role, string InternalUserId, string SessionContextKey, string DefaultDatabase);
}
