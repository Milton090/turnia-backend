using Microsoft.AspNetCore.Mvc;
using TurniaCore.Data.Constants;
using TurniaCore.Services.Device;
using Microsoft.AspNetCore.SignalR;
using TurniaCore.Services.SignalR;
using TurniaWeb.Areas.Queue.Dtos.Ticket;
using TurniaCore.Services.Queue;
using TurniaWeb.Areas.Devices.Dtos;
using TurniaCore.Services.SignalR.Interfaces;
using AutoMapper;
using System.Diagnostics;

namespace TurniaWeb.Areas.Devices.Controllers
{
    [Area("Devices")]
    [ApiController]
    [Route("/devices/[controller]/")]
    public class CheckinDeviceController : Controller
    {
        private readonly DeviceConfigurationService _deviceConfigurationService;
        private readonly QueueService _queueService;
        private readonly IHubContext<ViewerScreenService, IViewerScreenService> _signalRViewerScreenService;
        private readonly IHubContext<TurniaService, ITurniaService> _signalRTurniaService;
        private readonly IMapper _mapper;
        public CheckinDeviceController(DeviceConfigurationService deviceConfigurationService,
            QueueService queueService,
            IHubContext<ViewerScreenService, IViewerScreenService> signalRViewerScreenService, IHubContext<TurniaService, ITurniaService> signalRTurniaService, IMapper mapper
            )
        {
            _deviceConfigurationService = deviceConfigurationService;
            _queueService = queueService;
            _signalRViewerScreenService = signalRViewerScreenService;
            _signalRTurniaService = signalRTurniaService;
            _mapper = mapper;
        }

        [HttpPost("checkindeviceconfigurations")]
        public async Task<IActionResult> CheckinDeviceConfiguration([FromHeader(Name = ConstantDefinitions.DeviceKeyHeaderDefinition)] string deviceKey)
        {
            var result = await _deviceConfigurationService.GetCheckinDeviceConfigurationAsync(deviceKey);
            if (result.Item1 == false)
            {
                return NotFound();
            }
            return Ok(result.Item2);
        }

        [HttpGet("services")]
        public async Task<IActionResult> GetServices([FromHeader(Name = ConstantDefinitions.DeviceKeyHeaderDefinition)] string deviceKey)
        {
            var result = await _deviceConfigurationService.GetCheckinDeviceServicesAsync(deviceKey);
            var servicesList = new ServicesListDto();

            var mapService = _mapper.Map<List<ServiceDto>>(result.Item2);

            servicesList.Services = mapService;

            return Ok(servicesList);
        }


        [HttpPost("tickets")]
        public async Task<IActionResult> AddTicket(AddTicketDto model)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var result = await _queueService.CreateTicketAsync(model.DeviceId, model.DeviceKey, model.Service);
            if (result.Item1)
            {
                #region SignalR
                await _signalRViewerScreenService.Clients
                        .All
                        .RefreshAttendingList($"Ticket {result.Item3?.TicketNumber} has been created");

                await _signalRTurniaService.Clients
                        .All
                        .RefreshAttendingList($"Ticket has been created");


                await _signalRTurniaService.Clients
                        .All
                        .TicketCreated(result.Item3!.Id, result.Item3.TicketNumber);
                #endregion

                Console.WriteLine($"Create Ticket: {stopWatch.Elapsed.TotalMilliseconds} ms");
                return Ok(result.Item3);
            }
            return BadRequest(result.Item2);
        }

        [HttpPost("tickets/{id}/markprinted")]
        public async Task<IActionResult> MarkTicketAsPrinted(MarkTicketPrintDto model)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var result = await _queueService.MarkTicketAsPrintedAsync(model.Id, model.DeviceKey);
            if (result.Item1)
            {

                Console.WriteLine($"MarkTicketAsPrinted: {stopWatch.Elapsed.TotalMilliseconds} ms");
                return Ok(result.Item3);
            }
            return BadRequest(result.Item2);
        }
    }
}
