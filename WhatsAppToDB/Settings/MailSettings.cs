    public class MailSettings
    {
        public string SmtpServer { get; set; } = "";
        public string Port { get; set; } = "587";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public bool EnableSsl { get; set; } = true;
    }