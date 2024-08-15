using Flurl.Http;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LOGIN.LogsCouchDBServices
{
    public class CouchDBLogger
    {
        private readonly string _couchDbUrl = "http://158.23.169.201:5984/logs";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _username = "admin";  
        private readonly string _password = "00227";  

        public CouchDBLogger(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string description, string data)
        {
            var logEntry = new
            {
                user = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonymous",
                action = action,
                description = description,
                data = data,
                timestamp = DateTime.UtcNow
            };

            try
            {
                await _couchDbUrl
                    .WithBasicAuth(_username, _password) // Agregar autenticación básica
                    .PostJsonAsync(logEntry);

                Console.WriteLine("Log guardado exitosamente.");
            }
            catch (FlurlHttpException flurlEx)
            {
                // Error específico de Flurl.Http
                Console.WriteLine($"Error en la solicitud HTTP a CouchDB: {flurlEx.Message}");
                Console.WriteLine($"Contenido de la respuesta: {await flurlEx.GetResponseStringAsync()}");
            }
            catch (Exception ex)
            {
                // Otros errores generales
                Console.WriteLine($"Error al guardar el log en CouchDB: {ex.Message}");
            }
        }
    }
}
