using System.Text.Json;

namespace WhatsAppToDB
{
    public class WhatsAppService
    {
        public async Task SendWhatsAppResponse(string to, string text, WhatsAppSettings waSettings, IWhatsAppLogger waLogger)
        {            
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", waSettings.Token);

                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = to,
                    type = "text",
                    text = new { body = text }
                };
                Console.WriteLine($"WhatsApp {to} {text}");
                if (to != "000")
                {
                    await client.PostAsJsonAsync($"https://graph.facebook.com/v16.0/{waSettings.PhoneId}/messages", payload);
                }
            }
            catch (Exception ex)
            {
                await waLogger.LogAsync($"Error in SendWhatsAppResponse: {ex.ToString()}");
            }
        }

        public async Task<(bool isSuccess, string to, string message)> GetWhatsAppMessage(string receivedBody, IWhatsAppLogger waLogger)
        {
            using var jsonDoc = JsonDocument.Parse(receivedBody);
            var entry = jsonDoc.RootElement.GetProperty("entry")[0];
            var changes = entry.GetProperty("changes")[0];
            var value = changes.GetProperty("value");

            if (value.TryGetProperty("messages", out var messages))
            {
                var message = messages[0];
                var senderPhone = message.GetProperty("from").GetString();
                var messageText = message.GetProperty("text").GetProperty("body").GetString();

                if (!string.IsNullOrWhiteSpace(messageText) && !string.IsNullOrEmpty(senderPhone))
                {
                    var logmsg = $"Received whatsapp message {messageText} from {senderPhone}";
                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {logmsg}");
                    await waLogger.LogAsync(senderPhone, logmsg);
                    return (true, senderPhone, messageText);
                }
                else
                {
                    var logmsg = $"no message or phone received {senderPhone} {messageText}";
                    await waLogger.LogAsync(logmsg);
                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {logmsg}");
                    return (false, null, null);
                }
            }
            return (false, null, null);
        }
    }
}
