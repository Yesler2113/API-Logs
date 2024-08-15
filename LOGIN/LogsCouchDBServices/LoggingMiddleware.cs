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
            var request = context.Request;

            // Obtener el ID del usuario si está autenticado, de lo contrario usar "Usuario Desconocido"
            var userId = context.User.Identity.IsAuthenticated
                         ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         : "Usuario Desconocido";

            var timestamp = DateTime.UtcNow;

            string requestBodyContent = string.Empty;
            string requestInfo = $"Method: {request.Method}, Path: {request.Path}, QueryString: {request.QueryString}";

            // Solo leer el cuerpo de la solicitud si es un POST
            if (request.Method == HttpMethods.Post)
            {
                requestBodyContent = await ReadRequestBody(request);
                requestInfo += $", Body: {requestBodyContent}";
            }

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                string responseBodyContent = await ReadResponseBody(context.Response);
                string responseInfo;

                if (request.Method == HttpMethods.Get)
                {
                    int dataCount = CountItemsInResponse(responseBodyContent);
                    responseInfo = $"StatusCode: {context.Response.StatusCode}, DataCount: {dataCount}, Body: Informacion Obtenida";
                }
                else
                {
                    responseInfo = $"StatusCode: {context.Response.StatusCode}, Body: {responseBodyContent}";
                }

                var status = context.Response.StatusCode >= 200 && context.Response.StatusCode < 400 ? "success" : "error";
                var description = status;

                await _logger.LogAsync("Request", description, JsonConvert.SerializeObject(new
                {
                    UserId = userId,
                    Timestamp = timestamp,
                    Request = requestInfo,
                    Response = responseInfo,
                    Status = status
                }));

                // Restablecer la posición del cuerpo de la respuesta para que pueda ser leída por el cliente
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync("Request", "error", JsonConvert.SerializeObject(new
                {
                    UserId = userId,
                    Timestamp = timestamp,
                    Request = requestInfo,
                    Error = ex.Message,
                    Status = "error"
                }));

                throw;
            }
        }

        private async Task<string> ReadRequestBody(HttpRequest request)
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            request.Body.Seek(0, SeekOrigin.Begin);
            return Encoding.UTF8.GetString(buffer);
        }

        private async Task<string> ReadResponseBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            return text;
        }

        // Método para contar elementos en la respuesta JSON
        private int CountItemsInResponse(string responseBody)
        {
            var jsonArray = JsonConvert.DeserializeObject<dynamic>(responseBody) as IEnumerable<dynamic>;
            return jsonArray?.Count() ?? 0;
        }
    }
}
