using Microsoft.AspNetCore.Mvc;
using TurniaCore.Data.Constants;
using TurniaCore.Services.Device;
using TurniaCore.Services.Queue;
using TurniaWeb.Areas.Devices.Dtos;

namespace TurniaWeb.Areas.Devices.Controllers
{
    [Area("Devices")]
    [Route("/devices/[controller]")]
    public class ViewerScreenController : Controller
    {
        private readonly DeviceConfigurationService _deviceConfigurationService;
        private readonly QueueService _queueService;


        public ViewerScreenController(DeviceConfigurationService deviceConfigurationService, QueueService queueService)
        {
            _deviceConfigurationService = deviceConfigurationService;
            _queueService = queueService;
        }

        [HttpPost("GetTickets/getTicketForScreen")]
        public async Task<IActionResult> GetTickets([FromHeader(Name = ConstantDefinitions.DeviceKeyHeaderDefinition)] string deviceKey)
        {
            var result = await _deviceConfigurationService.GetViewerScreenDeviceConfigurationAsync(deviceKey);
            if (result.Item1 == false || result.Item2 == null)
            {
                return NotFound();
            }

            var tickets = await _queueService.GetTicketForScreenAsync(deviceKey);
            if (!tickets.Item1)
            {
                return NotFound();
            }
            var data = tickets.Item3;

            var response = new
            {
                CallingTickets = data?.CallingTickets?.Select(t => new
                {
                    t.TicketNumber,
                    Counter = new
                    {
                        t.Counter?.Name
                    }
                }),
                AttendingTickets = data?.AttendingTickets?.Select(t => new
                {
                    t.TicketNumber,
                    Counter = new
                    {
                        t.Counter?.Name
                    }
                })
            };
            return Ok(response);
        }


        #region Viewer Screen validation
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateViewerScreen(ViewerScreenValidationRequestDto model)
        {
            var result = await _deviceConfigurationService.GetViewerScreenDeviceConfigurationAsync(model.ScreenKey);
            if (result.Item1)
            {
                var response = new ViewerScreenValidationResponseDto()
                {
                    ColumnNumber = result.Item2!.ColumnNumber
                };
                return Ok(response);
            }
            return BadRequest();
        }


        #endregion
    }
}
