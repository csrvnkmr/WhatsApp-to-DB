using System.Runtime.CompilerServices;
using System.Text;

namespace WhatsAppToDB
{
    // ── Log levels in ascending severity order ───────────────────────────────
    public enum LogLevel
    {
        Debug = 0,  // Verbose dev-only detail
        Info = 1,  // General operational messages
        Warning = 2,  // Unexpected but recoverable
        Error = 3,  // Failures that need attention
        Fatal = 4   // Critical — app cannot continue
    }

    // ── Interface ────────────────────────────────────────────────────────────
    public interface ILogger
    {
        // Original methods — preserved for backward compatibility
        Task LogAsync(string phoneNumber, string message);
        Task LogAsync(string message);
        bool WriteToConsole { get; set; }

        // Log level methods
        Task LogDebugAsync(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        Task LogInfoAsync(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        Task LogWarningAsync(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        Task LogErrorAsync(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        Task LogFatalAsync(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);

        // Exception overloads
        Task LogWarningAsync(string message, Exception ex, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        Task LogErrorAsync(string message, Exception ex, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        Task LogFatalAsync(string message, Exception ex, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);

        // Minimum level filter — messages below this are ignored
        LogLevel MinimumLevel { get; set; }
    }

    // ── Implementation ───────────────────────────────────────────────────────
    public class AppLogger : ILogger
    {
        private readonly string _logFolder;
        private readonly SemaphoreSlim _lock = new(1, 1); // thread-safe async file writes

        public bool WriteToConsole { get; set; } = false;
        public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

        // Console colours per level
        private static readonly Dictionary<LogLevel, ConsoleColor> LevelColours = new()
        {
            [LogLevel.Debug] = ConsoleColor.Gray,
            [LogLevel.Info] = ConsoleColor.Cyan,
            [LogLevel.Warning] = ConsoleColor.Yellow,
            [LogLevel.Error] = ConsoleColor.Red,
            [LogLevel.Fatal] = ConsoleColor.Magenta
        };

        public AppLogger(string? logFolder = null)
        {
            _logFolder = logFolder ?? Path.Combine(AppContext.BaseDirectory, "Logs");
            if (!Directory.Exists(_logFolder))
                Directory.CreateDirectory(_logFolder);
        }

        // ── Core write ───────────────────────────────────────────────────────

        private async Task WriteAsync(LogLevel level, string message, Exception? ex,
                                      string caller, string file, int line)
        {
            if (level < MinimumLevel) return;

            var entry = FormatEntry(level, message, ex, caller, file, line);

            if (WriteToConsole)
                WriteColoured(level, entry);

            // All levels go to the daily combined log
            await AppendToFileAsync(DailyLogFile(), entry);

            // Warnings and above also go to a dedicated errors log for quick triage
            if (level >= LogLevel.Warning)
                await AppendToFileAsync(ErrorLogFile(), entry);
        }

        private async Task AppendToFileAsync(string filePath, string entry)
        {
            await _lock.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(filePath, entry, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppLogger] Failed to write log: {ex.Message}");
            }
            finally
            {
                _lock.Release();
            }
        }

        // ── Formatting ───────────────────────────────────────────────────────

        private static string FormatEntry(LogLevel level, string message,
                                          Exception? ex, string caller,
                                          string file, int line)
        {
            var sb = new StringBuilder();

            // [14:32:01] [INFO ] [MyMethod | MyFile.cs:42]  Message here
            sb.Append($"[{DateTime.Now:HH:mm:ss}]");
            sb.Append($" [{LevelLabel(level)}]");
            sb.Append($" [{caller} | {Path.GetFileName(file)}:{line}]");
            sb.Append($"  {message}");
            sb.AppendLine();

            if (ex != null)
            {
                sb.AppendLine($"  Exception : {ex.GetType().Name}: {ex.Message}");

                // Walk inner exceptions
                var inner = ex.InnerException;
                int depth = 1;
                while (inner != null && depth <= 5)
                {
                    sb.AppendLine($"  Inner[{depth}] : {inner.GetType().Name}: {inner.Message}");
                    inner = inner.InnerException;
                    depth++;
                }

                sb.AppendLine($"  StackTrace: {ex.StackTrace}");
            }

            return sb.ToString();
        }

        private static string LevelLabel(LogLevel level) => level switch
        {
            LogLevel.Debug => "DEBUG",
            LogLevel.Info => "INFO ",
            LogLevel.Warning => "WARN ",
            LogLevel.Error => "ERROR",
            LogLevel.Fatal => "FATAL",
            _ => "?    "
        };

        private static void WriteColoured(LogLevel level, string entry)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = LevelColours.GetValueOrDefault(level, ConsoleColor.White);
            Console.Write(entry);
            Console.ForegroundColor = prev;
        }

        // ── File names ───────────────────────────────────────────────────────

        private string DailyLogFile() =>
            Path.Combine(_logFolder, $"log_{DateTime.Now:yyyyMMdd}.txt");

        private string ErrorLogFile() =>
            Path.Combine(_logFolder, $"errors_{DateTime.Now:yyyyMMdd}.txt");

        // ── ILogger — level methods ──────────────────────────────────────────

        public Task LogDebugAsync(string message,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => WriteAsync(LogLevel.Debug, message, null, caller, file, line);

        public Task LogInfoAsync(string message,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => WriteAsync(LogLevel.Info, message, null, caller, file, line);

        public Task LogWarningAsync(string message,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => WriteAsync(LogLevel.Warning, message, null, caller, file, line);

        public Task LogErrorAsync(string message,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => WriteAsync(LogLevel.Error, message, null, caller, file, line);

        public Task LogFatalAsync(string message,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => WriteAsync(LogLevel.Fatal, message, null, caller, file, line);

        // ── Exception overloads ──────────────────────────────────────────────

        public Task LogWarningAsync(string message, Exception ex,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => WriteAsync(LogLevel.Warning, message, ex, caller, file, line);

        public Task LogErrorAsync(string message, Exception ex,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => WriteAsync(LogLevel.Error, message, ex, caller, file, line);

        public Task LogFatalAsync(string message, Exception ex,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => WriteAsync(LogLevel.Fatal, message, ex, caller, file, line);

        // ── Original methods — backward compatible ───────────────────────────

        public async Task LogAsync(string message)
        {
            string fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
            await AppendToFileAsync(
                Path.Combine(_logFolder, fileName),
                $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        public async Task LogAsync(string phoneNumber, string message)
        {
            string fileName = $"log_{phoneNumber}_{DateTime.Now:yyyyMMdd}.txt";
            await AppendToFileAsync(
                Path.Combine(_logFolder, fileName),
                $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
    }
}