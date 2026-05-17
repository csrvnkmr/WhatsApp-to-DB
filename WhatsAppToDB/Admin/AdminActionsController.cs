using Microsoft.AspNetCore.Mvc;
using WhatsAppToDB.Services;

namespace WhatsAppToDB.Admin.Controllers
{
    [ApiController]
    [Route("admin/actions")]
    public class AdminActionsController : ControllerBase
    {
        private readonly VectorSyncStatusStore _statusStore;
        private readonly VectorSyncJobService _syncJobService;

        public AdminActionsController(
            VectorSyncStatusStore statusStore,
            VectorSyncJobService syncJobService)
        {
            _statusStore = statusStore;
            _syncJobService = syncJobService;
        }

        [HttpGet("{database}/{entity}")]
        public IActionResult GetActions(
            string database,
            string entity)
        {
            if (!entity.Equals(
                    "vectorconfigurations",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Ok(Array.Empty<AdminActionItem>());
            }

            var status =
                _statusStore.Get(database);

            if (status.IsRunning)
            {
                return Ok(new[]
                {
                    new AdminActionItem
                    {
                        Label = "Synchronization",
                        Value = "Running. Check Status",
                        Action = $"/admin/actions/{database}/checksync",
                        Method = "POST"
                    }
                });
            }

            var value =
                status.Success == false
                    ? $"Start. Last failed: {status.Message}"
                    : "Start";

            return Ok(new[]
            {
                new AdminActionItem
                {
                    Label = "Synchronization",
                    Value = value,
                    Action = $"/admin/actions/{database}/startsync",
                    Method = "POST"
                }
            });
        }

        [HttpPost("{database}/startsync")]
        public IActionResult StartSync(string database)
        {
            var started =
                _syncJobService.Start(database);

            if (!started)
            {
                return Conflict(new
                {
                    message = "Synchronization is already running.",
                    status = _statusStore.Get(database)
                });
            }

            return Ok(new
            {
                message = "Synchronization started.",
                status = _statusStore.Get(database)
            });
        }

        [HttpPost("{database}/checksync")]
        public IActionResult CheckSync(string database)
        {
            return Ok(new
            {
                status = _statusStore.Get(database)
            });
        }
    }
}