using TurniaCore.Data.Dto.Services;
using TurniaCore.Entities.Platform.User;

namespace TurniaCore.Data.Dto.Tickets
{
    public class TicketDto
    {
        public Guid Id { get; set; }
        public string TicketNumber { get; set; } = null!;
        public string TicketLetter { get; set; } = null!;
        public int TicketNumberSequence { get; set; }

        public DateTime TicketDate { get; set; }
        public DateTime? TicketFirstCallDateTime { get; set; }
        public DateTime? TicketLatestCallDateTime { get; set; }
        public DateTime? TicketAttendingStartDateTime { get; set; }
        public DateTime? TicketCompletedDateTime { get; set; }
        public TicketStatusEnum TicketStatus { get; set; }
        public enum TicketStatusEnum
        {
            Unattended = 0,
            CallInProgress = 1,
            StartAttending = 2,
            FinishedAttending = 3,
            NoShow = 4,
            Cancelled = 5
        }
        public bool TicketIsPrinted { get; set; }


        public bool TicketPriority { get; set; } = false;


        public QueueUserProfile CalledBy { get; set; }

        public ServiceDto? Service { get; set; }
    }
}
