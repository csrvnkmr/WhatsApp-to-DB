namespace WhatsAppToDB.Models
{
    public class LoginUser
    {

        public string Username { get; set; }
        public string Password { get; set; }
        public string Fullname { get; set; }
        public string Role { get; set; }
        public string InternalUserId { get; set; }
        public string SessionContextKey { get; set; }
        public string DefaultDatabase { get; set; }
        public string WhatsAppNumber { get; set; }

    }

}
