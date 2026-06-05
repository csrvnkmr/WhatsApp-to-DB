using WhatsAppToDB.Data;

public class UserInstructionHelper
{
    public static async Task<(bool isHandled, ChatMessageDto? message)> CheckForUserInstruction(
        string messageText, long sessionid, string username, string dbName, string modulename,
        UserInstructionRepository _userInstructionRepo, ChatDbRepository _chatDbRepo)
    {
        var instructionCommand = UserInstructionParser.Parse(messageText);

        if (instructionCommand.Type == InstructionCommandType.Set)
        {
            await _userInstructionRepo.AddSessionAsync(
                sessionid, username, instructionCommand.Instruction);
            await _chatDbRepo.InsertMessageAsync(sessionid, "User", messageText, "", "", 
                dbName, "", "", modulename);
            var ackMsg = $"Got it — I'll remember: \"{instructionCommand.Instruction}\" for this session.";
            await _chatDbRepo.InsertMessageAsync(sessionid, "Assistant", ackMsg, "", "", 
                dbName, "", "", modulename);
            return (true, new ChatMessageDto { MessageText = ackMsg, SessionId = sessionid });
        }

        if (instructionCommand.Type == InstructionCommandType.Clear)
        {
            await _userInstructionRepo.ClearSessionAsync(sessionid);
            var ackMsg = "Session instructions cleared.";
            return (true, new ChatMessageDto { MessageText = ackMsg, SessionId = sessionid });
        }

        if (instructionCommand.Type == InstructionCommandType.Show)
        {
            var all = await _userInstructionRepo.GetAllActiveAsync(
                sessionid, username, dbName);
            var ackMsg = all.Count == 0
                ? "No active instructions."
                : "Active instructions:\n" + string.Join("\n", all.Select((i, n) => $"{n+1}. {i}"));
            return (true, new ChatMessageDto { MessageText = ackMsg, SessionId = sessionid });
        }    
        return (false, null);
    }

}