namespace TurniaCore.Services.SignalR.Interfaces
{
    public interface ITurniaService
    {
        Task CallingTicket(Guid ticketId, string ticketNumber);
        Task StopCallingTicket(Guid ticketId, string ticketNumber);
        Task StopAttendingTicket(Guid ticketId, string ticketNumber);
        Task TicketCreated(Guid ticketId, string ticketNumber);
        Task RefreshAttendingList(string temporaryReason);
        Task NoShowTicket();
    }
}
