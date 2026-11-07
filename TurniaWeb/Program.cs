using System.Text;
using System.Text.Json.Serialization;
using System.Reflection;

using Microsoft.AspNetCore.Authentication.Cookies;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

using TurniaCore.Data;
using TurniaCore.Services.User;
using TurniaCore.Data.Constants;
using TurniaCore.Entities.Platform.User;
using TurniaCore.Data.DataSeeding;
using TurniaCore.Services.Device;
using TurniaCore.Data.Entities.Platform.User;
using TurniaCore.Services.SignalR;
using TurniaCore.Data.Extensions;
using TurniaCore.Services.Authentication;
using TurniaCore.Services.Queue;
using System.IdentityModel.Tokens.Jwt;
using TurniaCore.Services.UserManagement;
using TurniaCore.Data.Interceptors;


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((context, options) => { options.AllowSynchronousIO = true; });
builder.WebHost.UseIISIntegration();
#region Configuration Settings from Appsettings.json
var enableSwagger = Convert.ToBoolean(builder.Configuration.GetSection("AppSettings").GetSection("EnableSwagger").Value);
var enableDataSeeding = Convert.ToBoolean(builder.Configuration.GetSection("AppSettings").GetValue<bool>("DataSeeding"));
var enableInterceptorQueryLogging = builder.Configuration.GetSection("AppSettings").GetValue<bool>("InterceptorEnabled");
var interceptorLoggingTreshold = builder.Configuration.GetSection("AppSettings").GetValue<int>("InterceptorLoggingTreshold");
var enableAutoMigrations = Convert.ToBoolean(builder.Configuration.GetSection("AppSettings").GetValue<bool>("EnableAutoMigrations"));
#region JWT Settings
var jwtKey = builder.Configuration.GetSection("AppSettings").GetSection("JwtKey").Value;
var jwtIssuer = builder.Configuration.GetSection("AppSettings").GetSection("JwtIssuer").Value;
var jwtAudience = builder.Configuration.GetSection("AppSettings").GetSection("JwtAudience").Value;
var JwtExpiryMinutes = builder.Configuration.GetSection("AppSettings").GetValue("JwtExpiryMinutes",0);
#endregion
var allowedOrigins = (builder.Configuration.GetSection("AppSettings").GetSection("AllowedOrigins").Value ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
#endregion

#region CORS
builder.Services.AddCors(o => o.AddDefaultPolicy(policyBuilder =>
{
    policyBuilder
        .WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders("WWW-Authenticate", "Token-Expired");
}));

#endregion

#region MVC & SWAGGER

builder.Services.AddControllersWithViews()
    .AddMvcOptions(o => { o.OutputFormatters.Add(new XmlSerializerOutputFormatter()); }).AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

if (enableSwagger)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(
        options =>
        {
            options.CustomSchemaIds(c => c.FullName);
            options.SwaggerDoc("v1",
                new OpenApiInfo()
                {
                    Version = "v1",

                    Title = "Turnia API"
                });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description =
                    "Escriba 'Bearer' [espacio] y luego su token. Ejemplo: 'Bearer 12345abcdef'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
            });


            options.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
            {
                new OpenApiSecurityScheme()
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
            });
        });
}

#endregion

#region MiscConfiguration

builder.Services.AddLogging();
builder.Services.AddHttpContextAccessor();

#endregion

#region Services
builder.Services.AddScoped<UserAccountService>();
builder.Services.AddScoped<SignInManagerExtension>();
builder.Services.AddScoped<QueueService>();
builder.Services.AddScoped<QueueConfigurationService>();
builder.Services.AddScoped<DeviceConfigurationService>();
builder.Services.AddScoped<UserManagementService>();
#endregion

#region Services - JWT Token Generation - Inject config parameters
builder.Services.AddScoped<JwtTokenGenerationService>(x => new JwtTokenGenerationService(jwtKey,jwtIssuer,jwtAudience,JwtExpiryMinutes));
#endregion


#region AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services
.AddAutoMapper(
        Assembly.GetExecutingAssembly());
#endregion


#region SIGNALR

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(3);
});

#endregion


#region EntityFramework

builder.Services.AddDbContext<QueueContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString(ConstantDefinitions.QueueConnectionStringName)!,
b =>
{
    b.MigrationsAssembly("TurniaCore");
    b.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null);
})
.AddInterceptors(new QueryInterceptor(enableInterceptorQueryLogging,interceptorLoggingTreshold)));

#endregion


#region IDENTITY & AUTHENTICATION

builder.Services.AddIdentity<QueueUserProfile, QueueUserRole>(options => { options.User.RequireUniqueEmail = true; })
    .AddEntityFrameworkStores<QueueContext>()
    .AddSignInManager<SignInManager<QueueUserProfile>>()
    .AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
    opt.TokenLifespan = TimeSpan.FromHours(2));

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 10;
    options.Password.RequiredUniqueChars = 1;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;

        var signingKey = jwtKey;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,

            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!)),
            ValidateLifetime = true,
            ValidateActor = false,
            ValidateTokenReplay = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException))
                {
                    var tokenExpiredHeader = "Token-Expired";
                    if(context.Response.Headers.TryGetValue(tokenExpiredHeader, out var headerValue))
                    {
                        context.Response.Headers[tokenExpiredHeader] = headerValue;
                    }
                    else
                    {
                        context.Response.Headers.TryAdd(tokenExpiredHeader, "true");
                    }

                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Headers["Location"] = context.RedirectUri;
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});


builder.Services.Configure<CookieAuthenticationOptions>(option =>
{
    option.LoginPath = PathString.Empty;
});


#endregion


#region APP_BUILD & Config
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseStaticFiles();

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

#region APP_BUILD & Config - SignalR
app.MapHub<TurniaService>("/signalr/turnia", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
});

app.MapHub<ViewerScreenService>("/signalr/viewerscreen", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
});

#endregion
#endregion

#region SwaggerForDevOnly
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DefaultModelsExpandDepth(-1);
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Turnia API");
    });
}
#endregion


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<QueueContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<QueueUserProfile>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<QueueUserRole>>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger<Program>();

    if (args.Contains("--reset-demo"))
    {
        logger.LogInformation("Reset demo: deleting database");
        await dbContext.Database.EnsureDeletedAsync();
    }

    #region AutoMigrate

    if (enableAutoMigrations)
    {
        logger.LogInformation($"AutoMigrations enabled: run migrations");
        await dbContext.ApplyPendingMigrationsAsync();
    }

    #endregion

    #region DataSeeding
    if (enableDataSeeding)
    {
        logger.LogInformation("Starting dataseeding....");
        await BasicDataSeeding.SeedAsync(dbContext, userManager, roleManager, loggerFactory, 5);
    }
    else
    {
        logger.LogInformation("Dataseeding disabled by appsettings config");
    }
    #endregion
}

app.Run();
