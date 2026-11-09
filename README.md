# Turnia Backend

API del prototipo Turnia, un sistema de gestión de turnos con kiosco, interfaz para trabajadores y pantalla de TV. Se encarga de los usuarios y roles, los servicios, los counters y los tickets, y avisa en tiempo real a la TV y a los trabajadores cuando algo cambia.

El proyecto se completa con [turnia-frontend](https://github.com/Milton090/turnia-frontend) y [turnia-kiosk](https://github.com/Milton090/turnia-kiosk).

La API está publicada en https://turnia-api.onrender.com y su Swagger en https://turnia-api.onrender.com/swagger

## Tecnologías

ASP.NET Core 8, Entity Framework Core 8 con PostgreSQL, ASP.NET Identity con JWT, SignalR y Swagger.

## Estructura

`TurniaCore` tiene las entidades, el contexto de datos, las migraciones, los datos de prueba y los servicios con la lógica del sistema. `TurniaWeb` tiene los controladores, los hubs de SignalR y la configuración.

## Cómo correrlo

Se necesita el SDK de .NET 8, o el 10 con el runtime de ASP.NET Core 8, y PostgreSQL en localhost con el usuario postgres y la contraseña postgres.

```powershell
cd TurniaWeb
dotnet run
```

La primera vez crea la base de datos `turnia`, aplica las migraciones y carga los datos de prueba. La API queda en http://localhost:6101 y Swagger en http://localhost:6101/swagger.

Para volver los datos al estado inicial está `dotnet run -- --reset-demo`, que borra la base y la crea de nuevo.

## Configuración

Los valores de desarrollo están en `TurniaWeb/appsettings.Development.json`, así que si PostgreSQL tiene otra contraseña basta con cambiar ahí la cadena de conexión. Fuera de desarrollo los valores se pasan como variables de entorno, como `ConnectionStrings__Queue`, `AppSettings__JwtKey`, `AppSettings__AllowedOrigins` o `AppSettings__EnableSwagger`.

## Datos de prueba

| Usuario | Contraseña | Rol |
|---|---|---|
| admin@turnia.local | Admin2026* | Administrador |
| trabajador1@turnia.local | Turnia2026* | Trabajador en el Counter 1 |
| trabajador2@turnia.local | Turnia2026* | Trabajador en el Counter 2 |

Los servicios son Atención general, Caja y Asesoría. El Counter 1 atiende Atención general y Asesoría, y el Counter 2 atiende Caja y Asesoría. La clave del kiosco es `turnia-kiosco-demo` y la de la pantalla es `turnia-pantalla-demo`.

## Endpoints

Todo está documentado en Swagger. Las rutas de `/api/Authentication` y `/api/UserManagement` manejan la sesión y los usuarios, las de `/queue` los servicios, counters y tickets, y las de `/devices` son las que usan el kiosco y la TV con su clave de dispositivo. Los hubs de SignalR están en `/signalr/turnia` y `/signalr/viewerscreen`.

Para crear un ticket sin el kiosco se puede usar `POST /devices/CheckinDevice/tickets` con un body como este, cambiando `service` por el id del servicio que devuelve `GET /devices/CheckinDevice/services`.

```json
{
  "deviceId": "00000000-0000-0000-0000-000000000000",
  "deviceKey": "turnia-kiosco-demo",
  "service": "ID-DEL-SERVICIO"
}
```
