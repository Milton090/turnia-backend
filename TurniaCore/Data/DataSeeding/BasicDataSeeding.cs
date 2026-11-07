using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TurniaCore.Data.Constants;
using TurniaCore.Data.Entities.Platform.Devices;
using TurniaCore.Data.Entities.Platform.User;
using TurniaCore.Data.Entities.App.Queue;
using TurniaCore.Entities.Platform.User;

namespace TurniaCore.Data.DataSeeding
{
    public static class BasicDataSeeding
    {
        public const string DemoDeviceKey = "turnia-kiosco-demo";
        public const string DemoScreenKey = "turnia-pantalla-demo";

        public static async Task SeedAsync(QueueContext context, UserManager<QueueUserProfile> userManager, RoleManager<QueueUserRole> roleManager,
            ILoggerFactory loggerFactory, int? retry = 0)
        {
            if (retry != null)
            {
                var retryForAvailability = retry.Value;
                try
                {
                    await SeedRoleAsync(roleManager, IdentityDefinitions.QueueWebAppAdmin, "Administra usuarios, servicios y counters");
                    await SeedRoleAsync(roleManager, IdentityDefinitions.QueueWebAppUser, "Atiende los tickets de su counter");

                    var serviceGeneral = await SeedServiceAsync(context, "Atención general", "Consultas y trámites generales", "A", 1);
                    var serviceCaja = await SeedServiceAsync(context, "Caja", "Pagos y facturación", "C", 2);
                    var serviceAsesoria = await SeedServiceAsync(context, "Asesoría", "Orientación personalizada", "S", 3);

                    var counter1 = await SeedCounterAsync(context, "Counter 1", "Atención general y asesoría", serviceGeneral, serviceAsesoria);
                    var counter2 = await SeedCounterAsync(context, "Counter 2", "Caja y asesoría", serviceCaja, serviceAsesoria);

                    if (!await context.DeviceConfigurationCheckin.AnyAsync())
                    {
                        await context.DeviceConfigurationCheckin.AddAsync(new DeviceConfigurationCheckin
                        {
                            Name = "Kiosco principal",
                            DeviceKey = DemoDeviceKey
                        });
                    }

                    if (!await context.DeviceConfigurationViewerScreens.AnyAsync())
                    {
                        await context.DeviceConfigurationViewerScreens.AddAsync(new DeviceConfigurationViewerScreens
                        {
                            Name = "Pantalla principal",
                            ScreenKey = DemoScreenKey,
                            ColumnNumber = 2
                        });
                    }

                    await context.SaveChangesAsync();

                    await SeedUserAsync(userManager, "admin@turnia.local", "Admin2026*", "Ana", "Administradora", IdentityDefinitions.QueueWebAppAdmin, null);
                    await SeedUserAsync(userManager, "trabajador1@turnia.local", "Turnia2026*", "Carlos", "Pérez", IdentityDefinitions.QueueWebAppUser, counter1.Id);
                    await SeedUserAsync(userManager, "trabajador2@turnia.local", "Turnia2026*", "Laura", "Gómez", IdentityDefinitions.QueueWebAppUser, counter2.Id);
                }
                catch (Exception ex)
                {
                    if (retryForAvailability >= 10) throw;
                    retryForAvailability++;
                    var log = loggerFactory.CreateLogger<QueueContext>();
                    log.LogError(ex.Message);
                    await SeedAsync(context, userManager, roleManager, loggerFactory, retryForAvailability);
                }
            }
        }

        private static async Task SeedRoleAsync(RoleManager<QueueUserRole> roleManager, string name, string description)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                await roleManager.CreateAsync(new QueueUserRole { Name = name, Description = description });
            }
        }

        private static async Task<Service> SeedServiceAsync(QueueContext context, string name, string description, string ticketCode, int sortIndex)
        {
            var service = await context.Services.FirstOrDefaultAsync(s => s.Name == name);
            if (service == null)
            {
                service = new Service
                {
                    Name = name,
                    Description = description,
                    TicketCode = ticketCode,
                    SortIndex = sortIndex,
                    IsActive = true
                };
                await context.Services.AddAsync(service);
            }
            return service;
        }

        private static async Task<Counter> SeedCounterAsync(QueueContext context, string name, string description, params Service[] services)
        {
            var counter = await context.Counters.FirstOrDefaultAsync(c => c.Name == name);
            if (counter == null)
            {
                counter = new Counter
                {
                    Name = name,
                    Description = description,
                    IsActive = true,
                    Services = services.ToList()
                };
                await context.Counters.AddAsync(counter);
            }
            return counter;
        }

        private static async Task SeedUserAsync(UserManager<QueueUserProfile> userManager, string email, string password, string firstName, string lastName, string role, Guid? counterId)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                return;
            }

            user = new QueueUserProfile
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Active = true,
                Created = DateTime.Now,
                AssignedCounterId = counterId
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
