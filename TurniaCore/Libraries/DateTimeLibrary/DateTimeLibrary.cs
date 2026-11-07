namespace TurniaCore.Libraries.DateTimeLibrary
{
    public class DateTimeLibrary
    {
        private const int TimeZone = -5;

        public static DateTime Now => DateTime.UtcNow.AddHours(TimeZone);
    }
}
