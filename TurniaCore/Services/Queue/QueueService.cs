using Microsoft.EntityFrameworkCore;
using TurniaCore.Data;
using TurniaCore.Data.Dto.Tickets;
using TurniaCore.Libraries.DateTimeLibrary;
using TurniaCore.Services.Device;
using TurniaCore.Services.User;
using TurniaCore.Data.Entities.App.Queue;
using TurniaCore.Entities.Platform.User;
using TurniaCore.Data.Dto.Counters;
using TurniaCore.Data.Dto.User;
using System.Diagnostics;
using AutoMapper;

namespace TurniaCore.Services.Queue
{
    public class QueueService
    {
        private readonly QueueContext _queueContext;
        private readonly DeviceConfigurationService _deviceConfigurationService;
        private readonly QueueConfigurationService _queueConfigurationService;
        private readonly UserAccountService _userAccountService;
        private readonly IMapper _mapper;


        public QueueService(QueueContext queueuContext, DeviceConfigurationService deviceConfigurationService, UserAccountService userAccountService, QueueConfigurationService queueConfigurationService, IMapper mapper)
        {
            _queueContext = queueuContext;
            _queueConfigurationService = queueConfigurationService;
            _deviceConfigurationService = deviceConfigurationService;
            _userAccountService = userAccountService;
            _mapper = mapper;
        }


        #region Counters
        public async Task<(bool, List<CounterDto>?)> GetActiveCountersWithAssignedUsersAsync(bool? overrideCounterName = false)
        {
            var result = await _queueContext
                .Counters
                .Include(c => c.LoggedInUser)
                .OrderBy(s => s.Name)
                .Where(c => c.IsActive)
                .ToListAsync();
            if (result == null)
            {
                return (false, null);
            }

            var counterDtos = new List<CounterDto>();
            foreach (var counter in result)
            {
                var counterDto = new CounterDto()
                {
                    Id = counter.Id,
                    Description = counter.Description,
                    IsActive = counter.IsActive,

                    Services = counter.Services,
                    Tickets = counter.Tickets,
                    LoggedInUser = counter.LoggedInUser,
                };
                if (overrideCounterName.GetValueOrDefault())
                {
                    if (counter.LoggedInUser == null)
                    {
                        counterDto.Name = counter.Name;
                    }
                    else
                    {
                        counterDto.Name = $"{counter.Name} ({counter.LoggedInUser.FirstName} {counter.LoggedInUser.LastName})";
                    }
                }
                else
                {
                    counterDto.Name = counter.Name;
                }

                counterDtos.Add(counterDto);
            }

            return (true, counterDtos);
        }

        public async Task<bool> AssignUserToCounter(Guid counterId, int? ticketOption)
        {
            Counter? currentCounter;

            #region Get User
            var user = await _userAccountService.GetMyUserProfileAsync();
            if (user == null)
            {
                return false;
            }
            #endregion

            #region Get Counter
            var newCounter = await _queueContext.Counters
                .Include(c => c.LoggedInUser)
                .FirstOrDefaultAsync(c => c.Id == counterId);
            if (newCounter == null)
            {
                return false;
            }

            if (newCounter.IsActive == false)
            {
                return false;
            }
            #endregion

            #region Current Counter - Check if attending ticket
            if (user.AssignedCounter != null)
            {
                currentCounter = await _queueContext.Counters.FirstOrDefaultAsync(c => c.Id == user.AssignedCounter.Id);
                if (currentCounter != null)
                {
                    var ticketsCalling = await GetCurrentlyCallingTicket();
                    var ticketsAttending = await GetCurrentlyAttendingTicket();
                    if (ticketsCalling == null && ticketsAttending == null)
                    {
                        currentCounter.LoggedInUser = null;
                    }
                }
            }
            #endregion


            #region New Counter - Check if attending ticket
            if (newCounter.LoggedInUser != null)
            {
                var userOnNewCounter = await _queueContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == newCounter.LoggedInUser.Id);

                if (userOnNewCounter != null)
                {
                    var ticketsCalling = await GetCurrentlyCallingTicket(userOnNewCounter);
                    var ticketsAttending = await GetCurrentlyAttendingTicket(userOnNewCounter);
                    if (ticketsCalling == null && ticketsAttending == null)
                    {
                        newCounter.LoggedInUser = user;
                    }
                    else if (ticketsCalling != null || ticketsAttending != null)
                    {
                        switch (ticketOption)
                        {

                            case 1:

                                #region Complete Ticket
                                var actionResult = await CompleteTicketAsyncFromOtherCounter(counterId);
                                if (actionResult.Item1)
                                {
                                    newCounter.LoggedInUser = user;
                                    var result3 = await _queueContext.SaveChangesAsync();
                                    if (result3 > 0)
                                    {
                                        return true;
                                    }
                                }
                                break;
                            #endregion


                            case 2:

                                #region Requeue Ticket
                                var ticket = await _queueContext.Tickets.FirstOrDefaultAsync(x => x.Id == ticketsCalling!.Id);
                                ticket!.TicketStatus = Ticket.TicketStatusEnum.Unattended;
                                ticket!.CalledBy = null;
                                ticket!.TicketPriority = true;
                                newCounter.LoggedInUser = user;
                                var result1 = await _queueContext.SaveChangesAsync();
                                if (result1 > 0)
                                {
                                    return true;
                                }
                                break;
                            #endregion


                            case 3:

                                #region No show Ticket
                                var ticket2 = await _queueContext.Tickets.FirstOrDefaultAsync(x => x.Id == ticketsCalling!.Id);
                                ticket2!.TicketStatus = Ticket.TicketStatusEnum.NoShow;
                                newCounter.LoggedInUser = user;
                                var result2 = await _queueContext.SaveChangesAsync();
                                if (result2 > 0)
                                {
                                    return true;
                                }
                                break;
                            #endregion

                            default:
                                return false;
                        }
                    }
                }
                else
                {
                    newCounter.LoggedInUser = user;
                }
            }
            else
            {
                newCounter.LoggedInUser = user;
            }
            #endregion

            var result = await _queueContext.SaveChangesAsync();

            if (result > 0)
            {
                return true;
            }

            return false;
        }

        public async Task<(bool, string?)> UnAssignUserFromCounter()
        {
            var user = await _userAccountService.GetMyUserProfileAsync();
            if (user == null)
            {
                return (false, "No se encontró el usuario");
            }

            if (user.AssignedCounter == null)
            {
                return (true, $"El usuario no tiene un counter asignado");
            }

            var ticketsCalling = await GetCurrentlyCallingTicket();
            var ticketsAttending = await GetCurrentlyAttendingTicket();
            if (ticketsCalling != null || ticketsAttending != null)
            {
                return (false, $"El usuario está llamando o atendiendo un ticket: Llamando: {ticketsCalling?.TicketNumber}, Atendiendo: {ticketsAttending?.TicketNumber}");
            }
            user.AssignedCounterId = null;
            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                return (true, $"Usuario desasignado del counter");
            }

            return (true, $"No se pudo desasignar el counter");
        }

        #endregion

        #region Tickets
        public async Task<(bool, string, Ticket?)> CreateTicketAsync(Guid deviceId, string deviceKey, Guid service)
        {
            var deviceUsed = await _deviceConfigurationService.GetCheckinDeviceConfigurationAsync(deviceKey);

            if (deviceUsed.Item2 == null)
            {
                return (false, $"El dispositivo usado para crear el ticket no es válido", null);
            }

            var serviceDefinition = await _queueConfigurationService.GetServiceAsync(service);

            if (serviceDefinition.Item2 == null || serviceDefinition.Item1 == false)
            {
                return (false, $"Servicio inválido", null);
            }

            #region Service - check if active
            if (serviceDefinition.Item2.IsActive == false)
            {
                return (false, $"El servicio está desactivado, no se puede crear el ticket", null);
            }
            #endregion

            #region Ticket - Creation
            var sequence = await GenerateTicketNumberForServiceAsync(serviceDefinition.Item2);

            if (sequence.Item1 == false)
            {
                return (false, $"Error al generar el número del ticket", null);
            }

            var newTicket = new Ticket
            {
                TicketDate = DateTimeLibrary.Now,
                Service = serviceDefinition.Item2,
                TicketStatus = Ticket.TicketStatusEnum.Unattended,
                TicketIsPrinted = false,
                TicketNumberSequence = sequence.Item2,
                TicketLetter = sequence.Item3,
                TicketNumber = sequence.Item4,
                Device = deviceUsed.Item2,
            };
            #endregion


            await _queueContext.Tickets.AddAsync(newTicket);
            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                return (true, newTicket.TicketNumber, newTicket);
            }
            return (false, $"No se pudo crear el ticket", null);
        }


        public async Task<(bool, string, Ticket?)> MarkTicketAsPrintedAsync(Guid ticketId, string deviceKey)
        {

            var deviceUsed = await _deviceConfigurationService.GetCheckinDeviceConfigurationAsync(deviceKey);
            if (deviceUsed.Item2 == null)
            {
                return (false, $"El dispositivo usado para crear el ticket no es válido", null);
            }

            var ticket = await _queueContext.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null)
            {
                return (false, $"Id de ticket inválido", null);
            }
            ticket.TicketIsPrinted = true;

            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                return (true, $"Ticket {ticket.TicketNumber} marcado como impreso", ticket);
            }
            return (false, $"El ticket {ticket.TicketNumber} NO fue marcado como impreso", null);
        }


        public async Task<(bool, string, TicketViewDto?)> GetTicketForScreenAsync(string deviceKey)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var deviceUsed = await _deviceConfigurationService.GetViewerScreenDeviceConfigurationAsync(deviceKey);
            ;
            if (deviceUsed.Item1 == false || deviceUsed.Item2 == null)
            {
                return (false, $"Viewersceen cannot be null", null);
            }

            var tickets = await _queueContext.Tickets
                .AsNoTracking()
                .Include(d => d.Counter)
                .Include(d => d.Service)
                .Where(t => t.TicketDate.Date == DateTimeLibrary.Now.Date)
                .Where(d => d.TicketStatus == Ticket.TicketStatusEnum.StartAttending || d.TicketStatus == Ticket.TicketStatusEnum.CallInProgress)
                .Select(t => new Ticket
                {
                    TicketNumber = t.TicketNumber,
                    Counter = t.Counter != null ? new Counter
                    {
                        Id = t.Counter.Id,
                        Name = t.Counter.Name
                    } : null,
                    TicketStatus = t.TicketStatus,
                    TicketAttendingStartDateTime = t.TicketAttendingStartDateTime,
                    TicketLatestCallDateTime = t.TicketLatestCallDateTime,
                })
                .ToListAsync();

            stopWatch.Stop();
            Console.WriteLine($"GetTicketForScreenAsync: {stopWatch.Elapsed.TotalSeconds}");


            var ticketViewResult = GetTicketForScreenHelperFunctionAsync(tickets);
            return (true, $"Grabbing list of tickets (v2)...", ticketViewResult.Item2);
        }

        private static (bool, TicketViewDto) GetTicketForScreenHelperFunctionAsync(List<Ticket> tickets)
        {
            TicketViewDto ticketView = new TicketViewDto();

            #region Calling Tickets
            var callingTickets = tickets
                .Where(x => x.TicketStatus == Ticket.TicketStatusEnum.CallInProgress)
                .OrderBy(x => x.TicketLatestCallDateTime)
                .ToList();
            ticketView.CallingTickets = callingTickets;
            #endregion

            #region Attending Tickets
            var attendingTickets = tickets
                .Where(x => x.TicketStatus == Ticket.TicketStatusEnum.StartAttending)
                .OrderBy(x => x.TicketAttendingStartDateTime)
                .ToList();
            ticketView.AttendingTickets = attendingTickets;
            #endregion

            return (true, ticketView);
        }

        public async Task<(bool, string?, TicketDto?)> CallNextTicketAsync()
        {
            var user = await _userAccountService.GetMyUserProfileAsync();
            if (user == null)
            {
                return (false, "No se encontró el usuario para asignarle el ticket", null);
            }

            if (user.AssignedCounter == null)
            {
                return (false, "El usuario no tiene un counter asignado", null);
            }

            var dateTimeNow = DateTimeLibrary.Now;

            var counter = await _queueContext.Counters
               .Include(x => x.Services)
               .Where(x => x.Services.Any())
               .FirstOrDefaultAsync(x => x.Id == user.AssignedCounter!.Id);

            if (counter == null)
            {
                return (false, "No se encontró un counter con servicios asignado al usuario", null);
            }

            if (counter.IsActive == false)
            {
                return (false, "El counter está cerrado", null);
            }

            var ticketsCalledButNotFinished = await _queueContext.Tickets
                .Include(t => t.CalledBy)
                .Where(t => t.CalledBy == user)
                .Where(t => t.TicketDate.Date == dateTimeNow.Date)
                .Where(t => t.TicketStatus == Ticket.TicketStatusEnum.CallInProgress || t.TicketStatus == Ticket.TicketStatusEnum.StartAttending)
                .ToListAsync();

            if (ticketsCalledButNotFinished != null && ticketsCalledButNotFinished.Count > 0)
            {
                var ticketText = string.Join(" | ", ticketsCalledButNotFinished.Select(t => t.TicketNumber));
                return (false, $"El usuario ya está atendiendo el ticket {ticketText}, no puede llamar el siguiente", null);
            }

            var tickets = await _queueContext.Tickets
                .Include(c => c.Service)
                .Include(x => x.CalledBy)
                .Include(x => x.Counter)
                .Where(t => t.TicketStatus == Ticket.TicketStatusEnum.Unattended)
                .Where(t => t.TicketDate.Date == dateTimeNow.Date)
                .Where(t => counter.Services.Contains(t.Service))
                .OrderBy(t => t.TicketDate)
                .ToListAsync();

            if (tickets == null || tickets.Count == 0)
            {
                return (false, "No hay tickets pendientes por llamar", null);
            }

            Console.WriteLine($"Ticket Counts {tickets.Count}");

            var prioTickets = tickets.Count(t => t.TicketPriority);
            var ticket = prioTickets > 0
                ? tickets.Where(t => t.TicketPriority).OrderBy(t => t.TicketDate).First()
                : tickets.OrderBy(t => t.TicketDate).First();

            if (ticket.TicketFirstCallDateTime == null)
            {
                ticket.TicketFirstCallDateTime = dateTimeNow;
            }
            ticket.TicketLatestCallDateTime = dateTimeNow;
            ticket.TicketStatus = Ticket.TicketStatusEnum.CallInProgress;
            ticket.CalledBy = user;
            ticket.Counter = counter;

            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                var ticketDto = _mapper.Map<TicketDto>(ticket);

                return (true, $"Ticket {ticket.TicketNumber}", ticketDto);
            }

            return (false, $"No se pudo actualizar el ticket {ticket.TicketNumber}", null);
        }

        public async Task<(bool, string?, TicketDto?)> StartNextTicketAsync()
        {
            var user = await _userAccountService.GetMyUserProfileAsync();
            if (user == null)
            {
                return (false, "No se encontró el usuario para asignarle el ticket", null);
            }

            if (user.AssignedCounter == null)
            {
                return (false, "El usuario no tiene un counter asignado", null);
            }

            var dateTimeNow = DateTimeLibrary.Now;

            var tickets = await _queueContext.Tickets
                .Include(x => x.CalledBy)
                .Where(t => t.TicketStatus == Ticket.TicketStatusEnum.CallInProgress)
                .Where(t => t.TicketDate.Date == dateTimeNow.Date)
                .Where(t => t.CalledBy == user)
                .OrderBy(t => t.TicketDate)
                .ToListAsync();

            if (tickets == null || tickets.Count == 0)
            {
                return (false, $"No hay tickets pendientes por llamar", null);
            }

            if (tickets.Count == 1)
            {
                var ticket = tickets.First();

                ticket.TicketAttendingStartDateTime = dateTimeNow;
                ticket.TicketStatus = Ticket.TicketStatusEnum.StartAttending;


                var result = await _queueContext.SaveChangesAsync();
                if (result > 0)
                {
                    var ticketDto = _mapper.Map<TicketDto>(ticket);

                    return (true, $"Ticket {ticket.TicketNumber}", ticketDto);
                }
                return (false, $"No se pudo actualizar el ticket {ticket.TicketNumber}", null);
            }
            return (false, $"El usuario tiene varios tickets en atención, no se puede continuar", null);
        }

        public async Task<(bool, string?, TicketDto?)> CompleteTicketAsync()
        {
            var user = await _userAccountService.GetMyUserProfileAsync();
            if (user == null)
            {
                return (false, "No se encontró el usuario para asignarle el ticket", null);
            }

            if (user.AssignedCounter == null)
            {
                return (false, "El usuario no tiene un counter asignado", null);
            }

            var dateTimeNow = DateTimeLibrary.Now;

            var ticket = await _queueContext.Tickets
                .Include(x => x.CalledBy)
                .Where(t => t.TicketStatus == Ticket.TicketStatusEnum.StartAttending)
                .Where(t => t.TicketDate.Date == dateTimeNow.Date)
                .Where(t => t.CalledBy == user)
                .OrderBy(t => t.TicketDate)
                .FirstOrDefaultAsync();

            if (ticket == null)
            {
                return (false, $"No se encontró un ticket en atención", null);
            }

            var now = DateTimeLibrary.Now;
            ticket.TicketCompletedDateTime = dateTimeNow;
            ticket.TicketStatus = Ticket.TicketStatusEnum.FinishedAttending;
            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                var ticketDto = _mapper.Map<TicketDto>(ticket);

                return (true, $"Ticket actualizado", ticketDto);
            }
            return (false, $"No se pudo actualizar el ticket", null);
        }


        public async Task<(bool, string, TicketListDto?)> GetTodaysTickets2Async()
        {
            var today = DateTimeLibrary.Now.Date;

            var tickets = await _queueContext.Tickets
                .AsNoTracking()
                .Include(d => d.Counter)
                .Include(d => d.Service)
                .Where(t => t.TicketDate.Date == today)
                .Where(t => t.TicketStatus == Ticket.TicketStatusEnum.Unattended ||
                            t.TicketStatus == Ticket.TicketStatusEnum.FinishedAttending ||
                            t.TicketStatus == Ticket.TicketStatusEnum.NoShow)
                .AsSplitQuery()
                .Select(t => new Ticket
                {
                    TicketNumber = t.TicketNumber,
                    TicketDate = t.TicketDate,
                    TicketStatus = t.TicketStatus,
                    TicketAttendingStartDateTime = t.TicketAttendingStartDateTime,
                    TicketCompletedDateTime = t.TicketCompletedDateTime,
                    TicketFirstCallDateTime = t.TicketFirstCallDateTime,
                    CalledBy = t.CalledBy,
                    Service = new Service
                    {
                        Id = t.Service.Id,
                        Name = t.Service.Name
                    },
                    ServiceId = t.Service.Id,
                })
                .ToListAsync();


            var unattendedTickets = tickets
                .Where(t => t.TicketStatus == Ticket.TicketStatusEnum.Unattended)
                .OrderBy(t => t.TicketDate)
                .ToList();

            var todaysTickets = tickets
                .Where(t => t.TicketStatus == Ticket.TicketStatusEnum.FinishedAttending || t.TicketStatus == Ticket.TicketStatusEnum.NoShow)
                .OrderByDescending(t => t.TicketDate)
                .ToList();


            var user = await _userAccountService.GetMyUserProfileAsync();
            List<Service> services = [];

            if (user!.AssignedCounterId != null)
            {
                services = await _userAccountService.GetServicesPerCounter(user!.AssignedCounterId);

            }
            var servicesIds = services.Select(s => s.Id).ToHashSet();


            var unattendedTicketsPerCounter = unattendedTickets
                .Where(x => servicesIds.Contains(x.ServiceId))
                .ToList();


            var ticketList = new TicketListDto
            {
                UnAttendedTickets = _mapper.Map<List<TicketDto>>(unattendedTickets),
                TodaysTickets = _mapper.Map<List<TicketDto>>(todaysTickets),
                UnAttendedTicketsPerCounter = _mapper.Map<List<TicketDto>>(unattendedTicketsPerCounter)
            };

            return (true, "Grabbing list of tickets...", ticketList);
        }

        public async Task<(bool, Ticket?)> GetLatestTicketForServiceAsync(Service service)
        {
            var latest = await _queueContext
                .Tickets
                .Include(t => t.Service)
                .Where(t => t.TicketDate.Date == DateTimeLibrary.Now.Date)
                .Where(t => t.Service == service)
                .OrderByDescending(t => t.TicketDate)
                .FirstOrDefaultAsync();

            if (latest == null)
            {
                return (true, null);
            }
            return (true, latest);
        }


        public async Task<Ticket?> GetCurrentlyCallingTicket(QueueUserProfile? user = null)
        {
            try
            {
                QueueUserProfile? userProfile;

                var dateTimeNow = DateTimeLibrary.Now;
                if (user == null)
                {
                    userProfile = await _userAccountService.GetMyUserProfileAsync();
                    if (userProfile == null)
                    {
                        return null;
                    }
                }
                else
                {
                    userProfile = user;
                }

                var tickets = await _queueContext.Tickets
                    .AsNoTracking()
                    .Include(t => t.Service)
                    .Where(x => x.TicketStatus == Ticket.TicketStatusEnum.CallInProgress)
                    .Where(t => t.TicketDate.Date == dateTimeNow.Date)
                    .Where(x => x.CalledBy == userProfile)
                    .OrderBy(t => t.TicketDate)
                    .FirstOrDefaultAsync();
                if (tickets == null)
                {
                    return null;
                }

                return tickets;
            }
            catch
            {
                return null!;
            }
        }

        public async Task<TicketDto?> GetCurrentlyAttendingTicket(QueueUserProfile? user = null)
        {
            try
            {
                QueueUserProfile? userProfile;

                var dateTimeNow = DateTimeLibrary.Now;
                if (user == null)
                {
                    userProfile = await _userAccountService.GetMyUserProfileAsync();
                    if (userProfile == null)
                    {
                        return null;
                    }
                }
                else
                {
                    userProfile = user;
                }

                var ticket = await _queueContext.Tickets
                    .AsNoTracking()
                    .Include(t => t.Service)
                    .Where(x => x.TicketStatus == Ticket.TicketStatusEnum.StartAttending)
                    .Where(t => t.TicketDate.Date == dateTimeNow.Date)
                    .Where(x => x.CalledBy == userProfile)
                    .OrderBy(t => t.TicketDate)
                    .FirstOrDefaultAsync();
                if (ticket == null)
                {
                    return null;
                }

                var ticketDto = _mapper.Map<TicketDto>(ticket);

                return ticketDto;
            }
            catch
            {
                return null!;
            }
        }


        public async Task<bool> NoShowTicketStatusChange()
        {
            try
            {
                var dateTimeNow = DateTimeLibrary.Now;
                var user = await _userAccountService.GetMyUserProfileAsync();

                if (user == null!)
                {
                    return false;
                }

                var ticket = await _queueContext.Tickets
                .Where(x => x.TicketStatus == Ticket.TicketStatusEnum.CallInProgress)
                .Where(t => t.TicketDate.Date == dateTimeNow.Date)
                .Where(x => x.CalledBy == user)
                .OrderBy(t => t.TicketDate)
                .FirstOrDefaultAsync();


                if (ticket == null)
                {
                    return false;
                }

                ticket.TicketCompletedDateTime = dateTimeNow;
                ticket.TicketStatus = Ticket.TicketStatusEnum.NoShow;
                var result = await _queueContext.SaveChangesAsync();

                if (result > 0)
                {
                    return true;
                }

                return false;

            }
            catch
            {
                return false!;
            }
        }

        #endregion


        #region Manage other Counter

        public async Task<(bool, string?, Ticket?)> CompleteTicketAsyncFromOtherCounter(Guid id)
        {
            var user = await _userAccountService.GetMyUserProfileAsync();
            if (user == null)
            {
                return (false, "No se encontró el usuario para asignarle el ticket", null);
            }

            if (user.AssignedCounter == null)
            {
                return (false, "El usuario no tiene un counter asignado", null);
            }

            var dateTimeNow = DateTimeLibrary.Now;

            var ticket = await _queueContext.Tickets
                .Include(x => x.Counter)
                .Where(t => t.TicketStatus == Ticket.TicketStatusEnum.StartAttending)
                .Where(t => t.TicketDate.Date == dateTimeNow.Date)
                .Where(t => t.Counter!.Id == id)
                .OrderBy(t => t.TicketDate)
                .FirstOrDefaultAsync();

            if (ticket == null)
            {
                return (false, $"No se encontró un ticket en atención", null);
            }

            var now = DateTimeLibrary.Now;
            ticket.TicketCompletedDateTime = dateTimeNow;
            ticket.TicketStatus = Ticket.TicketStatusEnum.FinishedAttending;
            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                ticket.Service = null!;
                ticket.Device = null!;
                ticket.Counter = null!;
                ticket.CalledBy = null!;

                return (true, $"Ticket actualizado", ticket);
            }
            return (false, $"No se pudo actualizar el ticket", null);
        }

        public async Task<CounterInfoDto> GetCounterInfo(Guid counterId)
        {
            var counter = await _queueContext.Counters
                                 .AsNoTracking()
                                 .Include(x => x.Tickets)
                                 .Include(x => x.LoggedInUser)
                                 .FirstOrDefaultAsync(x => x.Id == counterId);

            if (counter == null)
            {
                return null!;
            }

            var date = DateTimeLibrary.Now;
            var ticket = counter.Tickets.Where(x => x.TicketDate.Date == date.Date)
                                        .OrderByDescending(x => x.TicketDate)
                                        .FirstOrDefault();


            UserInfoDto userInfoDto = null!;

            if (counter.LoggedInUser != null)
            {
                userInfoDto = new UserInfoDto()
                {
                    FirstName = counter.LoggedInUser.FirstName,
                    LastName = counter.LoggedInUser.LastName,
                    Active = counter.LoggedInUser.Active,
                    AssignedCounterId = counter.LoggedInUser.AssignedCounterId

                };
            }


            TicketInfoDto ticketInfoDto = null!;
            if (ticket != null)
            {

                ticketInfoDto = new TicketInfoDto()
                {
                    Id = ticket.Id,
                    TicketStatus = (TicketInfoDto.TicketStatusEnum)ticket.TicketStatus,

                };
            }


            CounterInfoDto counterInfo = new CounterInfoDto()
            {
                Id = counter.Id,
                Name = counter.Name,
                Description = counter.Description,
                IsActive = counter.IsActive,
                TicketInfo = ticketInfoDto,
                QueueUserProfile = userInfoDto
            };

            return counterInfo;
        }

        #endregion

        #region SequenceNumberGenerator
        private async Task<(bool, int, string, string)> GenerateTicketNumberForServiceAsync(Service service)
        {
            var defaultSeqLength = 3;
            var seqNrToUse = 0;
            var ticketLetter = service.TicketCode;
            var ticketNumber = string.Empty;

            var result = await GetLatestTicketForServiceAsync(service);
            if (result.Item1 && result.Item2 != null)
            {
                seqNrToUse = result.Item2.TicketNumberSequence + 1;

                string numberStr = seqNrToUse.ToString();
                int numberOfDigits = numberStr.Length;
                if (numberOfDigits > defaultSeqLength)
                {
                    seqNrToUse = 1;
                }
            }
            else
            {
                seqNrToUse = 1;
            }

            ticketNumber = $"{service.TicketCode}-{seqNrToUse.ToString().PadLeft(defaultSeqLength, '0')}";

            if (!string.IsNullOrWhiteSpace(ticketNumber))
            {
                return (true, seqNrToUse, ticketLetter, ticketNumber);
            }
            return (false, 0, ticketLetter, ticketNumber);
        }
        #endregion

    }
}
