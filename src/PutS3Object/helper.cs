using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;

namespace S3Helper
{
    public class Functions
    {
        public async Task<APIGatewayProxyResponse> UploadFileToS3(string bucketName, string fileBody, string fileName, string fileContentType)
        {
            var s3Client = new AmazonS3Client();
            try
            {
                Console.WriteLine("[INFO] Uploading file to S3"+bucketName+fileBody+fileName);
                // Assuming the file content is sent in the request body as a base64 encoded string
                var fileContentBase64 = fileBody;
                var fileContentBytes = Convert.FromBase64String(fileContentBase64);
                // var fileName = "uploaded-file.txt"; // Change as needed or get from input

                var memoryStream = new MemoryStream(fileContentBytes);
            
                var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = memoryStream,
                        Key = fileName,
                        BucketName = bucketName,
                        ContentType = fileContentType
                    };

                    var fileTransferUtility = new TransferUtility(s3Client);
                      await fileTransferUtility.UploadAsync(uploadRequest);

                                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = JsonConvert.SerializeObject(new
                    {
                        message = "File uploaded successfully",
                        bucketName = bucketName,
                        fileName = fileName,
                        fileContentType = fileContentType
                    })
                    
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Error]"+ex);
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = $"Error uploading file: {ex.Message}"
                };
            }
        }
    }
}
