public static class UserInstructionParser
{
    private const string SetPrefix = "!set ";
    private const string ClearCommand = "!clear";
    private const string ShowCommand = "!instructions";

    public static UserInstructionCommand Parse(string input)
    {
        var trimmed = input.Trim();

        if (trimmed.StartsWith(SetPrefix, StringComparison.OrdinalIgnoreCase))
            return new UserInstructionCommand
            {
                Type = InstructionCommandType.Set,
                Instruction = trimmed[SetPrefix.Length..].Trim()
            };

        if (trimmed.Equals(ClearCommand, StringComparison.OrdinalIgnoreCase))
            return new UserInstructionCommand { Type = InstructionCommandType.Clear };

        if (trimmed.Equals(ShowCommand, StringComparison.OrdinalIgnoreCase))
            return new UserInstructionCommand { Type = InstructionCommandType.Show };

        return new UserInstructionCommand { Type = InstructionCommandType.None };
    }
}

public class UserInstructionCommand
{
    public InstructionCommandType Type { get; set; }
    public string Instruction { get; set; } = "";
}

public enum InstructionCommandType { None, Set, Clear, Show }