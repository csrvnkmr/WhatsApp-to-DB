using Microsoft.AspNetCore.Mvc;
using WhatsAppToDB.Constants;
using WhatsAppToDB.Services;

namespace WhatsAppToDB.Controllers
{
    [ApiController]
    [Route("api/user-instructions")]
    public class UserInstructionController : ControllerBase
    {
        private readonly UserInstructionRepository _repository;

        public UserInstructionController(UserInstructionRepository repository)
        {
            _repository = repository;
        }

        private string GetCurrentUserName()
        {
            return HttpContext.Items[ContextItems.UserName]?.ToString() ?? string.Empty;
        }

        private IActionResult ValidateUserName(string userName)
        {
            var result = UserService.ValidateUserName(userName);
            if (!result.isSuccess)
            {
                return Unauthorized();
            }
            return null!;
        }

        [HttpPost]
        public async Task<IActionResult> AddInstruction([FromBody] UserInstruction instruction)
        {
            var userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Unauthorized();
            }

            var validationResult = UserService.ValidateUserName(userName);
            if (!validationResult.isSuccess)
            {
                return Unauthorized();
            }

            instruction.Username = userName;
            instruction.Database ??= string.Empty;

            await _repository.AddPersistentAsync(instruction);
            return Created(string.Empty, instruction);
        }

        [HttpGet]
        public async Task<IActionResult> GetInstructions([FromQuery] string databaseId)
        {
            var userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(databaseId))
            {
                return BadRequest(new { error = "username and databaseId are required" });
            }

            var validationResult = UserService.ValidateUserName(userName);
            if (!validationResult.isSuccess)
            {
                return Unauthorized();
            }

            var instructions = await _repository.GetPersistentAsync(userName, databaseId);
            return Ok(instructions);
        }

        [HttpGet("database/{databaseId}")]
        [HttpGet("{databaseId}")]
        public async Task<IActionResult> GetInstructionsByDatabaseId([FromRoute] string databaseId)
        {
            var userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(databaseId))
            {
                return BadRequest(new { error = "username and databaseId are required" });
            }

            var validationResult = UserService.ValidateUserName(userName);
            if (!validationResult.isSuccess)
            {
                return Unauthorized();
            }

            var instructions = await _repository.GetPersistentAsync(userName, databaseId);
            return Ok(instructions);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetInstruction(int id)
        {
            var userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Unauthorized();
            }

            var validationResult = UserService.ValidateUserName(userName);
            if (!validationResult.isSuccess)
            {
                return Unauthorized();
            }

            var instruction = await _repository.GetPersistentAsync(id);
            if (instruction == null || !string.Equals(instruction.Username, userName, StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            return Ok(instruction);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteInstruction(int id)
        {
            var userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Unauthorized();
            }

            var validationResult = UserService.ValidateUserName(userName);
            if (!validationResult.isSuccess)
            {
                return Unauthorized();
            }

            var instruction = await _repository.GetPersistentAsync(id);
            if (instruction == null || !string.Equals(instruction.Username, userName, StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            await _repository.RemovePersistentAsync(id);
            return Ok(new { success = true });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateInstruction([FromBody] UserInstruction instruction)
        {
            var userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Unauthorized();
            }

            var validationResult = UserService.ValidateUserName(userName);
            if (!validationResult.isSuccess)
            {
                return Unauthorized();
            }

            var existing = await _repository.GetPersistentAsync((int)instruction.Id);
            if (existing == null || !string.Equals(existing.Username, userName, StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            instruction.Username = userName;
            instruction.Database ??= string.Empty;

            await _repository.UpdatePersistentAsync(instruction);
            return Ok(instruction);
        }

        [HttpGet("clear")]
        public async Task<IActionResult> ClearInstructions([FromQuery] string Database)
        {
            var userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(Database))
            {
                return BadRequest(new { error = "username and databaseId are required" });
            }

            var validationResult = UserService.ValidateUserName(userName);
            if (!validationResult.isSuccess)
            {
                return Unauthorized();
            }

            await _repository.ClearPersistentAsync(userName, Database);
            return Ok(new { success = true });
        }
    }
}
