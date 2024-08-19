using Flurl.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LOGIN.LogsCouchDBServices
{
    public class CouchDBLogService
    {
        private readonly string _couchDbUrl;
        private readonly string _username;
        private readonly string _password;

        public CouchDBLogService(IConfiguration configuration)
        {
            _couchDbUrl = configuration["CouchDbConnection:Url"];
            _username = configuration["CouchDbConnection:Username"];
            _password = configuration["CouchDbConnection:Password"];
        }

        public async Task<List<dynamic>> GetLogsAsync(int limit = 10, int skip = 0)
        {
            try
            {
                var response = await ($"{_couchDbUrl}/_all_docs?include_docs=true&limit={limit}&skip={skip}")
                    .WithBasicAuth(_username, _password)
                    .GetJsonAsync<CouchDBResponse>();

                var result = response.Rows.Select(row => row.Doc).ToList();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener los logs de CouchDB: {ex.Message}");
                throw;
            }
        }


        public class CouchDBResponse
        {
            [JsonProperty("rows")]
            public List<Row> Rows { get; set; }
        }

        public class Row
        {
            [JsonProperty("doc")]
            public dynamic Doc { get; set; }
        }

    }
}

