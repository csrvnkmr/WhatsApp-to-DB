public class UserInstruction
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string Database { get; set; } = "";
    public string InstructionText { get; set; } = "";
    public bool IsActive { get; set; }
}