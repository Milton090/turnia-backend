using Microsoft.AspNetCore.Mvc;
using TurniaWeb.Areas.Queue.Dtos.Counter;
using TurniaWeb.Areas.Queue.Dtos.Service;
using TurniaWeb.Areas.Queue.Dtos.Ticket;
using AutoMapper;
using TurniaWeb.Areas.Devices.Dtos;
using Microsoft.AspNetCore.Authorization;
using TurniaCore.Data.Constants;
using TurniaCore.Services.Queue;
using TurniaCore.Services.SignalR.Interfaces;
using TurniaCore.Services.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace TurniaWeb.Areas.Queue.Controllers
{
    [Authorize]
    [Area("Queue"), ApiController, Route("/queue/")]
    public class QueueController : Controller
    {
        private readonly QueueService _queueService;
        private readonly QueueConfigurationService _queueConfigurationService;
        private readonly IHubContext<TurniaService, ITurniaService> _signalRTurniaService;
        private readonly IHubContext<ViewerScreenService, IViewerScreenService> _signalRViewerScreenService;
        private readonly IMapper _mapper;

        public QueueController(QueueService queueService, QueueConfigurationService queueConfigurationService,
            IHubContext<TurniaService, ITurniaService> signalRTurniaService,
        IHubContext<ViewerScreenService, IViewerScreenService> signalRViewerScreenService,
            IMapper mapper)
        {
            _queueService = queueService;
            _queueConfigurationService = queueConfigurationService;
            _signalRTurniaService = signalRTurniaService;
            _signalRViewerScreenService = signalRViewerScreenService;
            _mapper = mapper;
        }

        #region Counters
        [HttpGet("counters")]
        public async Task<IActionResult> GetCounters()
        {
            var result = await _queueConfigurationService.GetCountersAsync();
            var mapper = _mapper.Map<List<Devices.Dtos.CounterDto>>(result.Item2);
            return Ok(mapper);
        }

        [HttpPost("counters")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> AddCounter(AddCounterDto model)
        {
            var result = await _queueConfigurationService.CreateCounterAsync(model.Name, model.Description, model.IsActive, model.Services);
            if (result.Item1)
            {
                var mapper = _mapper.Map<Devices.Dtos.CounterDto>(result.Item3);
                return Ok(mapper);
            }
            return BadRequest(result.Item2);
        }

        [HttpPost("counters/{id}/update")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> UpdateCounter(Guid id, UpdateCounterDto model)
        {
            var result = await _queueConfigurationService.UpdateCounterAsync(id, model.Name, model.Description, model.IsActive, model.Services);
            if (result.Item1)
            {
                return Ok(result.Item2);
            }
            return BadRequest(result.Item2);
        }

        [HttpPost("counters/{counterId}/link/{serviceId}")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> LinkCounterToService(Guid counterId, Guid serviceId)
        {
            var result = await _queueConfigurationService.LinkCounterToServiceAsync(counterId, serviceId);
            if (result.Item1)
            {
                return Ok(result.Item2);
            }
            return BadRequest(result.Item2);
        }

        [HttpPost("counters/{counterId}/unlink/{serviceId}")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> UnLinkCounterToService(Guid counterId, Guid serviceId)
        {
            var result = await _queueConfigurationService.UnLinkCounterToServiceAsync(counterId, serviceId);
            if (result.Item1)
            {
                return Ok(result.Item2);
            }
            return BadRequest(result.Item2);
        }

        [HttpGet("counters/active")]
        public async Task<IActionResult> GetActiveCountersWithAssignedUsers()
        {
            var result = await _queueService.GetActiveCountersWithAssignedUsersAsync(true);
            return Ok(result.Item2);
        }

        [HttpGet("counters/{counterId}/status")]
        public async Task<IActionResult> CounterInfo(Guid counterId)
        {
            var result = await _queueService.GetCounterInfo(counterId);
            if (result == null)
            {
                return BadRequest();
            }

            var mapper = _mapper.Map<CounterInfoDto>(result);
            return Ok(mapper);
        }

        [HttpPost("counters/status")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> ToggleCounterActivation(Guid counterId, [FromBody] StatusDto status)
        {
            try
            {
                var result = await _queueConfigurationService.ToggleCounterActivation(counterId, status.Status);
                if (result.Item1)
                {
                    return Ok(result.Item2);
                }

                return BadRequest(result.Item2);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("counters/{counterId}/assign")]
        public async Task<IActionResult> AssignUserToCounter(Guid counterId, [FromBody] TicketOption? ticketOption)
        {
            try
            {
                var result = await _queueService.AssignUserToCounter(counterId, ticketOption?.Option);
                if (result)
                {
                    return Ok("Usuario asignado al counter");
                }

                return BadRequest("No se pudo asignar el usuario al counter");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("counters/unassign")]
        public async Task<IActionResult> UnAssignUserFromCounter()
        {
            try
            {
                var result = await _queueService.UnAssignUserFromCounter();
                if (result.Item1)
                {
                    return Ok(result.Item2);
                }

                return BadRequest(result.Item2);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        #region Services
        [HttpPost("services")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> AddServicePost(AddServiceDto model)
        {
            var result = await _queueConfigurationService.CreateServiceAsync(model.Name, model.Description, model.TicketCode, model.IsActive);
            if (result.Item1)
            {
                return Ok(result.Item2);
            }
            return StatusCode(500);
        }

        [HttpPost("services/{id}/update")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> UpdateService(Guid id, UpdateServiceDto model)
        {
            var result = await _queueConfigurationService.UpdateServiceAsync(id, model.Name, model.Description, model.TicketCode, model.IsActive);
            if (result.Item1)
            {
                return Ok(result.Item2);
            }
            return BadRequest(result.Item2);
        }


        [HttpGet("services")]
        public async Task<IActionResult> GetServices()
        {
            var result = await _queueConfigurationService.GetServicesAsync();
            return Ok(result.Item2);
        }

        [HttpPost("service/status")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> ToggleServiceActivation(Guid serviceId, [FromBody] StatusDto status)
        {
            try
            {
                var result = await _queueConfigurationService.ToggleServiceActivation(serviceId, status.Status);
                if (result.Item1)
                {
                    return Ok(result.Item2);
                }

                return BadRequest(result.Item2);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Tickets

        [HttpGet("tickets/today")]
        public async Task<IActionResult> GetTodaysTicketsVer2()
        {
            var result = await _queueService.GetTodaysTickets2Async();

            if (!result.Item1)
                return BadRequest(result.Item2);

            var unattended = result.Item3?.UnAttendedTickets?
                .Select(t => new
                {
                    t.TicketDate,
                    t.TicketNumber,
                    Service = new
                    {
                        t.Service?.Name
                    }
                });

            var todays = result.Item3?.TodaysTickets?
                .Select(t => new
                {
                    t.TicketNumber,
                    t.TicketAttendingStartDateTime,
                    t.TicketCompletedDateTime,
                    t.TicketStatus,
                    t.TicketDate,
                    t.TicketFirstCallDateTime,
                    CalledBy = t.CalledBy.FirstName,
                    Service = new
                    {
                        t.Service?.Name
                    }
                });

            var unattendedPerCounter = result.Item3?.UnAttendedTicketsPerCounter?
                .Select(t => new
                {
                    t.TicketDate,
                    t.TicketNumber,
                    Service = new
                    {
                        t.Service?.Name
                    }
                });

            return Ok(new
            {
                UnAttendedTickets = unattended,
                TodaysTickets = todays,
                UnAttendedTicketsPerCounter = unattendedPerCounter
            });
        }

        [HttpGet("tickets/currentlycalling")]
        public async Task<IActionResult> GetCurrentlyCallingTicket()
        {
            try
            {
                var result = await _queueService.GetCurrentlyCallingTicket();
                if (result == null)
                {
                    return NoContent();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("tickets/currentlyattending")]
        public async Task<IActionResult> GetCurrentlyAttendingTicket()
        {
            try
            {
                var result = await _queueService.GetCurrentlyAttendingTicket();
                if (result == null)
                {
                    return NoContent();
                }


                return Ok(new
                {
                    result.TicketStatus,
                    result.TicketNumber,
                    Service = new
                    {
                        result.Service.Id
                    }
                });

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("tickets/noshow")]
        public async Task<IActionResult> NoShow()
        {
            try
            {
                var result = await _queueService.NoShowTicketStatusChange();
                if (result)
                {
                    #region SignalR
                    await _signalRViewerScreenService.Clients
                        .All
                        .RefreshAttendingList("A ticket has been set as no-show");

                    await _signalRTurniaService.Clients
                        .All
                        .NoShowTicket();

                    await _signalRTurniaService.Clients
                        .All
                        .RefreshAttendingList("A ticket has been set as no-show");
                    #endregion

                    return Ok("El estado del ticket cambió a \"No se presentó\"");
                }

                return BadRequest("No se pudo cambiar el estado del ticket");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("tickets/callnext")]
        public async Task<IActionResult> CallNextTicket()
        {
            var result = await _queueService.CallNextTicketAsync();
            if (result.Item1)
            {
                #region SignalR
                await _signalRViewerScreenService.Clients
                    .All
                    .CallingTicket(result.Item3!.Id, result.Item3.TicketNumber);

                await _signalRViewerScreenService.Clients
                    .All
                    .RefreshAttendingList("A ticket has been called");

                await _signalRTurniaService.Clients
                    .All
                    .CallingTicket(result.Item3!.Id, result.Item3.TicketNumber);

                await _signalRTurniaService.Clients
                    .All
                    .RefreshAttendingList("A ticket has been called");

                #endregion

                return Ok(new { result.Item3.TicketNumber });
            }
            return BadRequest(result.Item2);
        }

        [HttpPost("tickets/startnext")]
        public async Task<IActionResult> StartNextTicket()
        {
            var result = await _queueService.StartNextTicketAsync();
            if (result.Item1)
            {
                #region SignalR
                await _signalRViewerScreenService.Clients
                    .All
                    .StopCallingTicket(result.Item3!.Id, result.Item3.TicketNumber);

                await _signalRViewerScreenService.Clients
                    .All
                    .RefreshAttendingList("A ticket has been started");

                await _signalRTurniaService.Clients
                   .All
                   .StopCallingTicket(result.Item3!.Id, result.Item3.TicketNumber);

                await _signalRTurniaService.Clients
                    .All
                    .RefreshAttendingList("A ticket has been started");
                #endregion

                return Ok();
            }
            return BadRequest(result.Item2);
        }

        [HttpPost("tickets/completeticket")]
        public async Task<IActionResult> CompleteAttendingTicket()
        {
            var result = await _queueService.CompleteTicketAsync();
            if (result.Item1)
            {
                #region SignalR
                await _signalRViewerScreenService.Clients
                    .All
                    .RefreshAttendingList("A ticket has been completed");

                await _signalRTurniaService.Clients
                   .All
                   .StopAttendingTicket(result.Item3!.Id, result.Item3.TicketNumber);

                await _signalRTurniaService.Clients
                   .All
                   .RefreshAttendingList("A ticket has been completed");
                #endregion

                return Ok();
            }
            return BadRequest(result.Item2);
        }

        #endregion
    }
}
