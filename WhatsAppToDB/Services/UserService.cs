using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace WhatsAppToDB.Services
{

    public class UserService
    {

        public UserService() { }

        public static async Task<(bool isSuccess, UserSession session)> ValidateLogin(JsonConfigService jsonConfigService, string username, string password)
        {
            //var users = JsonSerializer.Deserialize<List<dynamic>>(File.ReadAllText("users.json"));
            var users = jsonConfigService.GetUsers();
            var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user == null)
            {
                return (false, null);
            }

            var token = Guid.NewGuid().ToString(); // Simple token generation
            var session = new UserSession(token, user.Username, user.Role, user.InternalUserId, user.SessionContextKey, user.DefaultDatabase);

            // Save session to SQLite UserTokens table
            try
            {
                var repo = jsonConfigService.GetChatDbRepository();
                await repo.InsertUserTokensAsync(session);
            }
            catch
            {
                // If DB write fails, fall back to file storage for compatibility
                var tokenfilename = "token.json";
                var sessions = File.Exists(tokenfilename)
                    ? JsonSerializer.Deserialize<List<UserSession>>(File.ReadAllText(tokenfilename))
                    : new List<UserSession>();
                sessions.Add(session);
                File.WriteAllText(tokenfilename, JsonSerializer.Serialize(sessions));
            }

            return (true, session);
        }

        private static (bool isSuccess, Abstractions.IdentityContext? identity) GetIdentity(UserSession? session)
        {
            if (session == null) return (false, null);
            Abstractions.IdentityContext ic = new Abstractions.IdentityContext();

            ic.InternalUserId = session.InternalUserId;
            ic.Role = session.Role;
            ic.SessionContextKey = session.SessionContextKey;
            ic.UserName = session.Username;

            return (true, ic);
        }

        public static (bool isSuccess, Abstractions.IdentityContext? identity) ValidateUserName(string username, JsonConfigService jsonConfigService)
        {
            try
            {
                var repo = jsonConfigService.GetChatDbRepository();
                var session = repo.GetUserTokenByUsernameAsync(username).GetAwaiter().GetResult();
                return GetIdentity(session);
            }
            catch
            {
                return (false, null);
            }
        }

        public static (bool isSuccess, Abstractions.IdentityContext? identity) ValidateToken(string token, JsonConfigService jsonConfigService)
        {
            try
            {
                var repo = jsonConfigService.GetChatDbRepository();
                var session = repo.GetUserTokenAsync(token).GetAwaiter().GetResult();
                return GetIdentity(session);
            }
            catch
            {
                return (false, null);
            }
        }
    }

    public record LoginRequest(string Username, string Password);
    public record AskRequest(string Question, long? SessionId, bool isEval = false);
    public record UserSession(string Token, string Username, string Role, 
        string InternalUserId, string SessionContextKey, 
        string DefaultDatabase);
}
/*
        CREATE TABLE IF NOT EXISTS usertokens (
            Token TEXT PRIMARY KEY,
            Username TEXT NOT NULL,
            Role TEXT,
            InternalUserId TEXT,
            SessionContextKey TEXT,
            DefaultDatabase TEXT
        );
*/