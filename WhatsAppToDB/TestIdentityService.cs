using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB
{
    public class TestIdentityService : IIdentityService
    {
        private readonly Dictionary<string, IdentityContext> _testUsers;

        public TestIdentityService(Dictionary<string, IdentityContext> testUsers)
        {
            _testUsers = testUsers;
        }

        public Task<IdentityContext> GetIdentityAsync(string mobileNumber)
        {
            if (_testUsers.TryGetValue(mobileNumber, out var identity))
            {
                return Task.FromResult(identity);
            }

            // Fallback for unknown test numbers
            return Task.FromResult(new IdentityContext { WhatsAppNumber = mobileNumber, Role = "Guest" });
        }

        public void HydrateRolePermissions(IdentityContext identity)
        {
            throw new NotImplementedException();
        }
    }
}
