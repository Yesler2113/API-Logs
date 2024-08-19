using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;

namespace LOGIN.LogsCouchDBServices
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CouchDBLogger _logger;

        public LoggingMiddleware(RequestDelegate next, CouchDBLogger logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);  // Procesar la solicitud

                // Lee el contenido de la respuesta
                responseBody.Seek(0, SeekOrigin.Begin);
                string responseBodyContent = await new StreamReader(responseBody).ReadToEndAsync();

                if (context.Request.Method == HttpMethods.Get)
                {
                    // Contar la cantidad de registros en la respuesta
                    int dataCount = CountItemsInResponse(responseBodyContent);

                    // Log solo la cantidad de registros accedidos
                    await LogRequestAndResponse(context, dataCount);
                }
                else
                {
                    // Log completo para otros métodos (POST, PUT, DELETE, etc.)
                    await LogRequestAndResponse(context, responseBodyContent);
                }

                // Copia la respuesta original de nuevo al stream original
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones y log de error
                await LogRequestAndResponse(context, null, ex.Message);

                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                throw;
            }
        }

        private async Task LogRequestAndResponse(HttpContext context, object logData, string error = null)
        {
            var request = context.Request;

            var userId = context.User.Identity.IsAuthenticated
                ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : "Usuario Desconocido";

            var timestamp = DateTime.UtcNow;
            var requestBodyContent = request.Method == HttpMethods.Post ? await ReadRequestBody(request) : string.Empty;
            var requestInfo = $"Method: {request.Method}, Path: {request.Path}, QueryString: {request.QueryString}";

            var responseInfo = error != null
                ? $"Error: {error}"
                : request.Method == HttpMethods.Get
                    ? $"DataCount: {logData}"
                    : $"Body: {logData}";

            var logEntry = new
            {
                UserId = userId,
                Timestamp = timestamp,
                Request = requestInfo,
                Response = responseInfo,
                Status = context.Response.StatusCode >= 200 && context.Response.StatusCode < 400 ? "success" : "error"
            };

            // Guardar el log en CouchDB
            await _logger.LogAsync("Request", logEntry.Status, logEntry);
        }

        private async Task<string> ReadRequestBody(HttpRequest request)
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength ?? 0)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            request.Body.Seek(0, SeekOrigin.Begin);
            return Encoding.UTF8.GetString(buffer);
        }

        private int CountItemsInResponse(string responseBody)
        {
            try
            {
                var jsonArray = JsonConvert.DeserializeObject<dynamic>(responseBody) as IEnumerable<dynamic>;
                return jsonArray?.Count() ?? 0;
            }
            catch (JsonReaderException)
            {
                // Si la respuesta no es un array JSON, retorna 0
                return 0;
            }
        }



    }
}
