using Amazon.S3;
using Amazon.S3.Transfer;
using StreamCraftAPI.Interface;

namespace StreamCraftAPI.Service
{
    public class S3StorageService : IPermanentStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName = "your-bucket-name";
        private readonly string _cdnBaseUrl = "https://cdn.myproject.com";
        private readonly ILogger<S3StorageService> _logger;

        public S3StorageService(IAmazonS3 s3Client, ILogger<S3StorageService> logger)
        {
            _s3Client = s3Client;
            _logger = logger;
        }

        public async Task<string> UploadVideoAsync(string localFilePath, string fileName)
        {
            return await UploadToS3Async(localFilePath, $"videos/{fileName}");
        }

        public async Task<string> UploadThumbnailAsync(string localFilePath, string fileName)
        {
            return await UploadToS3Async(localFilePath, $"thumbnails/{fileName}");
        }

        private async Task<string> UploadToS3Async(string localPath, string key)
        {
            try
            {
                var fileTransferUtility = new TransferUtility(_s3Client);

                await fileTransferUtility.UploadAsync(localPath, _bucketName, key);
                _logger.LogInformation("Uploaded {Key} to S3", key);

                return $"{_cdnBaseUrl}/{key}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 upload failed for {Key}", key);
                throw; 
            }
        }
    }

}
