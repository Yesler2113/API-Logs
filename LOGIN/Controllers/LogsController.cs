using LOGIN.LogsCouchDBServices;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LOGIN.Controllers
{
    [Route("api/logs")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly CouchDBLogService _logService;

        public LogsController(CouchDBLogService logService)
        {
            _logService = logService;
        }

        [HttpGet]
        public async Task<ActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            int skip = (page - 1) * pageSize;
            var logs = await _logService.GetLogsAsync(limit: pageSize, skip: skip);
            return Ok(new { page, pageSize, logs });
        }

    }
}

