using Microsoft.EntityFrameworkCore;
using TurniaCore.Data;
using TurniaCore.Data.Entities.Platform.Devices;
using TurniaCore.Data.Entities.App.Queue;

namespace TurniaCore.Services.Device
{
    public class DeviceConfigurationService
    {
        private readonly QueueContext _context;
        public DeviceConfigurationService(QueueContext context)
        {
            _context = context;
        }

        #region Checkin Device Configuration
        public async Task<(bool, DeviceConfigurationCheckin?)> GetCheckinDeviceConfigurationAsync(string deviceKey)
        {
            var result = await _context.DeviceConfigurationCheckin
                .Where(d => !string.IsNullOrWhiteSpace(deviceKey))
                .FirstOrDefaultAsync(d => d.DeviceKey == deviceKey);
            if (result == null)
            {
                return (false, null);
            }

            return (true, result);
        }
        #endregion


        public async Task<(bool, List<Service>?)> GetCheckinDeviceServicesAsync(string deviceKey)
        {
            var result = await GetCheckinDeviceConfigurationAsync(deviceKey);

            if (result.Item1 == false || result.Item2 == null)
            {
                return (false, null);
            }

            #region Get Services
            var resultServices = await _context.Services
              .Where(s => s.IsActive)
              .OrderBy(s => s.SortIndex)
              .ToListAsync();
            #endregion

            if (resultServices == null)
            {
                return (false, null);
            }

            return (true, resultServices);
        }


        public async Task<(bool, DeviceConfigurationViewerScreens?)> GetViewerScreenDeviceConfigurationAsync(string deviceKey)
        {
            var result = await _context.DeviceConfigurationViewerScreens
                .FirstOrDefaultAsync(d => d.ScreenKey == deviceKey);
            if (result == null)
            {
                return (false, null);
            }

            return (true, result);
        }
    }
}
