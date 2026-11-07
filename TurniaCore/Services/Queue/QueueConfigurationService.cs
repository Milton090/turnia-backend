using Microsoft.EntityFrameworkCore;
using TurniaCore.Data;
using TurniaCore.Data.Entities.App.Queue;

namespace TurniaCore.Services.Queue
{
    public class QueueConfigurationService
    {
        private readonly QueueContext _queueContext;
        public QueueConfigurationService(QueueContext queueuContext)
        {
            _queueContext = queueuContext;
        }

        #region Counters

        public async Task<(bool, string?, Counter?)> CreateCounterAsync(string name, string? description, bool status, List<Guid>? servicesToLink)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, $"No se puede crear un counter sin nombre", null);
            }
            var trimmed = name.Trim();
            var counterExists = await _queueContext.Counters
                .Where(c => c.Name == name || c.Name == trimmed || c.Name.ToUpper() == trimmed.ToUpper())
                .FirstOrDefaultAsync();
            if (counterExists != null)
            {
                return (false, $"El counter ya existe", null);
            }

            var counter = new Counter
            {
                Name = trimmed,
                Description = description,
                IsActive = status,
                Services = new List<Service>()
            };

            if (servicesToLink != null)
            {
                foreach (var service in servicesToLink)
                {
                    var serviceFromDb = await _queueContext.Services.FirstOrDefaultAsync(s => s.Id == service);
                    if (serviceFromDb != null)
                    {
                        counter.Services.Add(serviceFromDb);
                    }
                }
            }


await _queueContext.AddAsync(counter);
            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                return (true, $"Counter {trimmed} creado", counter);
            }
            return (false, $"El counter {trimmed} NO fue creado", null);
        }

        public async Task<(bool, string?, Counter?)> UpdateCounterAsync(Guid id, string name, string? description, bool status, List<Guid>? servicesToLink)
        {
            var counterToEdit = await _queueContext.Counters.Include(x => x.Services).FirstOrDefaultAsync(c => c.Id == id);
            if (counterToEdit == null)
            {
                return (false, $"El counter a editar no existe", null);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, $"No se puede actualizar un counter sin nombre", null);
            }
            var trimmed = name.Trim();

            var counterExists = await _queueContext.Counters
                .Where(c => c.Name == name || c.Name == trimmed || c.Name.ToUpper() == trimmed.ToUpper())
                .Where(c => c.Id != id)
                .FirstOrDefaultAsync();
            if (counterExists != null)
            {
                return (false, $"El counter ya existe", null);
            }

            counterToEdit.Name = name;
            counterToEdit.Description = description;
            counterToEdit.IsActive = status;

            if (servicesToLink != null)
            {
                counterToEdit.Services = null!;
                counterToEdit.Services = new List<Service>();

                foreach (var service in servicesToLink)
                {
                    var serviceFromDb = await _queueContext.Services.FirstOrDefaultAsync(s => s.Id == service);
                    if (serviceFromDb != null)
                    {
                        counterToEdit.Services.Add(serviceFromDb);
                    }
                }
            }

            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                return (true, $"Counter {trimmed} actualizado", counterToEdit);
            }
            return (false, $"El counter {trimmed} NO fue actualizado", null);
        }

        public async Task<(bool, string?, Counter?)> LinkCounterToServiceAsync(Guid counterId, Guid serviceToLink)
        {
            var counterExists = await _queueContext.Counters
                .Where(c => c.Id == counterId)
                .FirstOrDefaultAsync();
            if (counterExists == null)
            {
                return (false, $"El counter no existe", null);
            }

            var serviceFromDb = await _queueContext.Services.FirstOrDefaultAsync(s => s.Id == serviceToLink);
            if (serviceFromDb == null)
            {
                return (false, $"El servicio no existe", null);
            }
            if (counterExists.Services == null)
            {
                counterExists.Services = new();
            }
            counterExists.Services.Add(serviceFromDb);
            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                return (true, $"Servicio {serviceFromDb.Name} agregado al counter {counterExists.Name}", counterExists);
            }
            return (false, $"El servicio {serviceFromDb.Name} NO fue agregado al counter {counterExists.Name}", null);
        }

        public async Task<(bool, string?, Counter?)> UnLinkCounterToServiceAsync(Guid counterId, Guid serviceToLink)
        {
            var counterExists = await _queueContext.Counters
                .Include(c => c.Services)
                .Where(c => c.Id == counterId)
                .FirstOrDefaultAsync();
            if (counterExists == null)
            {
                return (false, $"El counter no existe", null);
            }

            var serviceToUnlinked = counterExists.Services.FirstOrDefault(s => s.Id == serviceToLink);
            if (serviceToUnlinked == null)
            {
                return (false, $"El servicio no está asignado al counter", null);
            }

            counterExists.Services.Remove(serviceToUnlinked);

            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                return (true, $"Servicio {serviceToUnlinked.Name} quitado del counter {counterExists.Name}", counterExists);
            }
            return (false, $"El servicio {serviceToUnlinked.Name} NO fue quitado del counter {counterExists.Name}", null);
        }


        public async Task<(bool, List<Counter>?)> GetCountersAsync()
        {
            var result = await _queueContext
                .Counters
                .Include(s => s.Services)
                .OrderBy(s => s.Name)
                .ToListAsync();
            if (result == null)
            {
                return (false, null);
            }
            return (true, result);
        }

        public async Task<(bool, string)> ToggleCounterActivation(Guid counterId, bool status)
        {
            try
            {
                var counter = await _queueContext.Counters.FirstOrDefaultAsync(x => x.Id == counterId);
                if (counter == null)
                {
                    return (false, $"No se encontró el counter con Id: {counterId}");
                }

                if (status)
                {
                    counter.IsActive = true;
                    var result = await _queueContext.SaveChangesAsync();

                    if (result > 0)
                    {
                        return (true, "El counter fue abierto");
                    }

                    return (false, "No se pudo cambiar el estado del counter");

                }
                else if (!status)
                {
                    counter.IsActive = false;
                    var result = await _queueContext.SaveChangesAsync();

                    if (result > 0)
                    {
                        return (true, "El counter fue cerrado");
                    }

                    return (false, "No se pudo cambiar el estado del counter");
                }

                return (false, "Opción de estado inválida");
            }
            catch (Exception ex)
            {
                return (false, $"Ocurrió un error al cambiar el estado del counter.\nError: {ex.Message}");
            }
        }
        #endregion


        #region Services

        public async Task<(bool, Service?)> CreateServiceAsync(string name, string description, string ticketCode, bool isActive)
        {
            var service = new Service();
            service.Name = name;
            service.Description = description;
            service.TicketCode = ticketCode;
            service.IsActive = isActive;

            await _queueContext.AddAsync(service);
            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                return (true, service);
            }
            return (false, null);

        }

        public async Task<(bool, Service?)> UpdateServiceAsync(Guid id, string name, string description, string ticketCode, bool isActive)
        {
            var service = await _queueContext.Services.FirstOrDefaultAsync(s => s.Id == id);
            if (service == null)
            {
                return (false, null);
            }

            service.Name = name;
            service.Description = description;
            service.TicketCode = ticketCode;
            service.IsActive = isActive;

            var result = await _queueContext.SaveChangesAsync();
            if (result > 0)
            {
                return (true, service);
            }
            return (false, null);

        }

        public async Task<(bool, List<Service>?)> GetServicesAsync()
        {
            var result = await _queueContext
                .Services
                .OrderBy(s => s.SortIndex)
                .ThenBy(s => s.Name)
                .ToListAsync();
            if (result == null)
            {
                return (false, null);
            }
            return (true, result);
        }

        public async Task<(bool, string)> ToggleServiceActivation(Guid serviceId, bool status)
        {
            try
            {
                var service = await _queueContext.Services.FirstOrDefaultAsync(x => x.Id == serviceId);
                if (service == null)
                {
                    return (false, $"No se encontró el servicio con Id: {serviceId}");
                }

                if (status)
                {
                    service.IsActive = true;
                    var result = await _queueContext.SaveChangesAsync();

                    if (result > 0)
                    {
                        return (true, "El servicio fue activado");
                    }

                    return (false, "No se pudo cambiar el estado del servicio");

                }
                else if (!status)
                {
                    service.IsActive = false;
                    var result = await _queueContext.SaveChangesAsync();

                    if (result > 0)
                    {
                        return (true, "El servicio fue desactivado");
                    }

                    return (false, "No se pudo cambiar el estado del servicio");
                }

                return (false, "Opción de estado inválida");
            }
            catch (Exception ex)
            {
                return (false, $"Ocurrió un error al cambiar el estado del servicio.\nError: {ex.Message}");
            }
        }

        public async Task<(bool, Service?)> GetServiceAsync(Guid serviceId)
        {
            var services = await _queueContext
                .Services
                .Where(s => s.Id == serviceId)
                .OrderBy(s => s.SortIndex)
           .ThenBy(s => s.Name)
           .FirstOrDefaultAsync();
            if (services == null)
            {
                return (false, null);
            }
            return (true, services);
        }

        #endregion

    }
}
