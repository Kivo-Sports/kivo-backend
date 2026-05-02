using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using kivoBackend.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace kivoBackend.Application.Services
{
    public class ImageStorageService : IStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public ImageStorageService(IConfiguration configuration)
        {
            _bucketName = configuration["FIREBASE_BUCKET"] ?? "kivo-sports.firebasestorage.app";
            var credentialFileName = configuration["GOOGLE_APPLICATION_CREDENTIALS"];

            var path = Path.Combine(Directory.GetCurrentDirectory(), credentialFileName);

            if (File.Exists(path))
            {
                var credential = GoogleCredential.FromFile(path);
                _storageClient = StorageClient.Create(credential);
            }
            else
            {
                _storageClient = StorageClient.Create();
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var objectName = $"logos/{Guid.NewGuid()}_{fileName}";

            await _storageClient.UploadObjectAsync(_bucketName, objectName, contentType, fileStream);

            return $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{Uri.EscapeDataString(objectName)}?alt=media";
        }
    }
}