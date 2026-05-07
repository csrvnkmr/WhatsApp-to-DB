namespace WhatsAppToDB.Audit
{
    public static class AuditActions
    {
        public const string Login = "LOGIN";

        public const string Logout = "LOGOUT";

        public const string DatabaseChanged = "DATABASE_CHANGED";

        public const string ProviderChanged = "PROVIDER_CHANGED";

        public const string ModelChanged = "MODEL_CHANGED";
    }
}
