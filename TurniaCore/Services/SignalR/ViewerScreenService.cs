using TurniaCore.Data;
using TurniaCore.Data.Entities.Platform.Devices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace TurniaCore.Services.SignalR
{
    public class ViewerScreenService : Hub<Interfaces.IViewerScreenService>
    {
        private QueueContext _queueContext { get; set; }
        public ViewerScreenService(QueueContext queueContext) 
        {
            _queueContext = queueContext;
        }

        public override async Task OnConnectedAsync()
        {
            var token = GetAuthTokenfromConnectionRequest();
            if(token.Item1)
            {
                var screen = await GetViewerScreenFromToken(token.Item2);
                if(screen.Item1 && screen.Item2 != null)
                {
                    Console.WriteLine($"ViewerScreenService: Connection Id: {Context.ConnectionId}, screen {screen.Item2.Name}");
                }
                else
                {
                    Console.WriteLine($"ViewerScreenService: Connection Id: {Context.ConnectionId}, NO SCREEN");
                }
            }

            await base.OnConnectedAsync();
        }

        #region Authentication and Validation

        public (bool, string) GetAuthTokenfromConnectionRequest()
        {
            var req = Context.GetHttpContext()?.Request;
            if (req != null)
            {
                if (string.IsNullOrWhiteSpace(req.Query["access_token"]))
                {
                    Console.WriteLine($"ViewerScreenService: GetAuthTokenfromConnectionRequest IsNullOrWhiteSpace");
                    return (false, null!);
                }
            }
            return (true, req.Query["access_token"].ToString());
        }

        public async Task<(bool, DeviceConfigurationViewerScreens?)> GetViewerScreenFromToken(string token)
        {
            var viewerScreen = await _queueContext
                .DeviceConfigurationViewerScreens
                .FirstOrDefaultAsync(v => v.ScreenKey == token);
            if(viewerScreen == null)
            {
                Console.WriteLine($"ViewerScreenService: GetViewerScreenFromToken: {Context.ConnectionId} returned null");
                return (false, null);
            }

            return (true,  viewerScreen);
        }

        #endregion


        public async Task CallingTicket(Guid ticketId, string ticketNumber)
        {
            Console.WriteLine($"ViewerScreenService: CallingTicket: {ticketId} {ticketNumber}");
            await Clients.All.CallingTicket(ticketId,ticketNumber);
        }

        public async Task StopCallingTicket(Guid ticketId, string ticketNumber)
        {
            Console.WriteLine($"ViewerScreenService: StopCallingTicket: {ticketId} {ticketNumber}");
            await Clients.All.StopCallingTicket(ticketId, ticketNumber);
        }

        public async Task RefreshAttendingList(string temporaryReason)
        {
            Console.WriteLine($"ViewerScreenService: RefreshAttendingList: {temporaryReason}");
            await Clients.All.RefreshAttendingList(temporaryReason);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"ViewerScreenService: OnDisconnectedAsync: {Context.ConnectionId}: Exception: {exception?.Message}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
