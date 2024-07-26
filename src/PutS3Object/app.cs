using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PutS3Object
{
    public class FileData
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string FileInBase64 { get; set; }
    }

    public class Function
    {

        private static readonly HttpClient client = new HttpClient();
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            try
            {
                // Getting the env variable
                string bucketName = Environment.GetEnvironmentVariable("BucketName");
                string tableName = Environment.GetEnvironmentVariable("TableName");
                string dbName = Environment.GetEnvironmentVariable("DBName");
                string dbServer = Environment.GetEnvironmentVariable("DBServer");
                string dbUser = Environment.GetEnvironmentVariable("DBUser");
                string dbPassword = Environment.GetEnvironmentVariable("DBPassword");
                string dbPort = Environment.GetEnvironmentVariable("DBPort");

                string connectionString = $"Server={dbServer};Database={dbName};User ID={dbUser};Password={dbPassword};Port={dbPort};";

                Console.WriteLine($"API event: {apigProxyEvent}");
                Console.WriteLine($"Connection String {connectionString}");
                Console.WriteLine($"Body: {apigProxyEvent.Body}");

                var fileData = JsonConvert.DeserializeObject<FileData>(apigProxyEvent.Body);
                var fileName = fileData.FileName;
                var fileBody = fileData.FileInBase64;
                var fileContentType = fileData.ContentType;

                // Use fileName and fileBody as needed
                Console.WriteLine($"File Name: {fileName}");
                // helper object created
                var s3Helper = new S3Helper.Functions();
                var response = await s3Helper.UploadFileToS3(bucketName, fileBody, fileName, fileContentType);
                Console.WriteLine($"[Info]Response: {response.StatusCode}");
                if (response.StatusCode == 200)
                {
                    var uploadDate = DateTime.Now.ToString("yyyy-MM-dd");
                    Console.WriteLine($"[Info]Upload Date: {uploadDate}");
                    using var connection = new MySqlConnection(connectionString);
                    connection.Open();
                    var query = $"INSERT INTO {tableName} (file_name, bucket_name, file_type, uploaded_date) VALUES (@fileName, @bucketName, @fileType, @uploadedDate);";
                    Console.WriteLine($"[Info]Query: {query}");

                    using var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@fileName", fileName);
                    command.Parameters.AddWithValue("@bucketName", bucketName);
                    command.Parameters.AddWithValue("@fileType", fileContentType);
                    command.Parameters.AddWithValue("@uploadedDate", uploadDate);

                    command.ExecuteNonQuery();
                    Console.WriteLine("[Info]Data inserted into the database successfully.");
                    return response;
                }
                else
                {
                    return response;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new APIGatewayProxyResponse
                {
                    Body = JsonConvert.ToString(e.Message),
                    StatusCode = 400,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
        }
    }
}
