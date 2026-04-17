using System.Runtime.CompilerServices;

namespace WhatsAppToDB
{
    public interface IWhatsAppLogger
    {
        Task LogAsync(string phoneNumber, string message);
        Task LogAsync(string message);

        bool WriteToConsole { get; set; } 
    }

    public class WhatsAppLogger : IWhatsAppLogger
    {
        private readonly string _logFolder = Path.Combine(AppContext.BaseDirectory, "Logs");

        public bool WriteToConsole { get; set; }=false;

        public WhatsAppLogger()
        {
            if (!Directory.Exists(_logFolder)) Directory.CreateDirectory(_logFolder);
        }

        private async Task Log(string fileName, string message)
        {
            try
            {
                string filePath = Path.Combine(_logFolder, fileName);
                string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
                if (WriteToConsole)
                {
                    Console.WriteLine(logEntry);
                }
                await File.AppendAllTextAsync(filePath, logEntry);
            }
            catch (Exception ex) { 
                // ignore any exception in Logging.
                Console.WriteLine("Error in logging " + ex.ToString());
            }
        }

        public async Task LogAsync(string message)
        {
            string fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
            await Log(fileName, message);
        }

        public async Task LogAsync(string phoneNumber, string message)
        {
            string fileName = $"log_{phoneNumber}_{DateTime.Now:yyyyMMdd}.txt";
            await Log(fileName, message);
        }
    }
}
