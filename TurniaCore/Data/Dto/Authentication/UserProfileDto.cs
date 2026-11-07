namespace TurniaCore.Data.Dto.Authentication
{
    public class UserProfileDto
    {
        #region Personalia
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Username { get; set; } = null!;
        #endregion

        #region Counter Assignment
        public bool IsAssignedToCounter { get; set; }
        public Guid? CounterId { get; set; }
        public UserProfileCounterDto Counter { get; set; } = null!;
        #endregion

        #region Ticket Attending Information
        public enum TicketStatusEnum
        {
            Unattended = 0,
            CallInProgress = 1,
            StartAttending = 2,
            FinishedAttending = 3,
            NoShow = 4,
            Cancelled = 5
        }
        public TicketStatusEnum? TicketStatus { get; set; }
        public bool IsCallingTicket { get; set;}
        public bool IsAttendingTicket { get; set;}
        public Guid? TicketId { get; set; }
        public string? TicketNumber { get; set; }
        public DateTime? TicketAttendingStartDateTime { get; set; }
        #endregion
    }

    public class UserProfileCounterDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
