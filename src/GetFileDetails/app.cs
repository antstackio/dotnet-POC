using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System;
using MySql.Data.MySqlClient;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GetFileRecord
{

    public class Function
    {

        private static readonly HttpClient client = new HttpClient();
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            string tableName = Environment.GetEnvironmentVariable("TableName");
            string dbName = Environment.GetEnvironmentVariable("DBName");
            string dbServer = Environment.GetEnvironmentVariable("DBServer");
            string dbUser = Environment.GetEnvironmentVariable("DBUser");
            string dbPassword = Environment.GetEnvironmentVariable("DBPassword");
            string dbPort = Environment.GetEnvironmentVariable("DBPort");
            string connectionString = $"Server={dbServer};Database={dbName};User ID={dbUser};Password={dbPassword};Port={dbPort};";
            Console.WriteLine($"Connection String {connectionString}");
            string query = $"SELECT * FROM {tableName};";
            Console.WriteLine($"Query {query}");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            var result = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(row);
            }
            connection.Close();

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(result),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
