using Flurl.Http;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LOGIN.LogsCouchDBServices
{
 
    public class CouchDBLogger
    {
        private readonly string _couchDbUrl = "";
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        private readonly string _username = "";  
        private readonly string _password = "";  

        public CouchDBLogger(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _couchDbUrl = configuration["CouchDbConnection:Url"];
            _username = configuration["CouchDbConnection:Username"];
            _password = configuration["CouchDbConnection:Password"];

        }

        public async Task LogAsync(string action, string description, object data)
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
                    .WithBasicAuth(_username, _password)
                    .PostJsonAsync(logEntry);

                Console.WriteLine("Log guardado exitosamente.");
            }
            catch (FlurlHttpException flurlEx)
            {
                Console.WriteLine($"Error en la solicitud HTTP a CouchDB: {flurlEx.Message}");
                Console.WriteLine($"Contenido de la respuesta: {await flurlEx.GetResponseStringAsync()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar el log en CouchDB: {ex.Message}");
            }
        }
    }
}
