namespace TurniaCore.Services.SignalR.Interfaces
{
    public interface IViewerScreenService
    {
        Task CallingTicket(Guid ticketId, string ticketNumber);
        Task StopCallingTicket(Guid ticketId, string ticketNumber);
        Task RefreshAttendingList(string temporaryReason);
    }
}
