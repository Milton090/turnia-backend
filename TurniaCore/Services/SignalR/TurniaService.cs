using Microsoft.AspNetCore.SignalR;

namespace TurniaCore.Services.SignalR
{
    public class TurniaService : Hub<Interfaces.ITurniaService>
    {
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"TurniaService: Connection Id: {Context.ConnectionId}");

            await base.OnConnectedAsync();
        }

        public async Task CallingTicket(Guid ticketId, string ticketNumber)
        {
            Console.WriteLine($"TurniaService: CallingTicket: {ticketId} {ticketNumber}");
            await Clients.All.CallingTicket(ticketId, ticketNumber);
        }

        public async Task StopCallingTicket(Guid ticketId, string ticketNumber)
        {
            Console.WriteLine($"TurniaService: StopCallingTicket: {ticketId} {ticketNumber}");
            await Clients.All.StopCallingTicket(ticketId, ticketNumber);
        }

        public async Task StopAttendingTicket(Guid ticketId, string ticketNumber)
        {
            Console.WriteLine($"TurniaService: StopAttendingTicket: {ticketId} {ticketNumber}");
            await Clients.All.StopAttendingTicket(ticketId, ticketNumber);
        }

        public async Task TicketCreated(Guid ticketId, string ticketNumber)
        {
            Console.WriteLine($"TurniaService: TicketCreated: {ticketId} {ticketNumber}");
            await Clients.All.TicketCreated(ticketId, ticketNumber);
        }

        public async Task RefreshAttendingList(string temporaryReason)
        {
            Console.WriteLine($"TurniaService: RefreshAttendingList: {temporaryReason}");
            await Clients.All.RefreshAttendingList(temporaryReason);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"TurniaService: OnDisconnectedAsync: {Context.ConnectionId}: Exception: {exception?.Message}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task NoShowTicket()
        {
            Console.WriteLine($"TurniaService: Ticket set as no-show");
            await Clients.All.NoShowTicket();
        }
    }
}
