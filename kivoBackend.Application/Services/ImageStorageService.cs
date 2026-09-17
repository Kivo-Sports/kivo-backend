using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using kivoBackend.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace kivoBackend.Application.Services
{
    public class ImageStorageService : IStorageService
    {
        private readonly string _bucketName;
        private readonly string? _credentialPath;
        private StorageClient? _storageClient;

        public ImageStorageService(IConfiguration configuration)
        {
            _bucketName = configuration["FIREBASE_BUCKET"] ?? "kivo-sports.firebasestorage.app";
            var credentialFileName = configuration["GOOGLE_APPLICATION_CREDENTIALS"];

            _credentialPath = string.IsNullOrWhiteSpace(credentialFileName)
                ? null
                : Path.Combine(Directory.GetCurrentDirectory(), credentialFileName);
        }

        public Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
            => UploadFileAsync(fileStream, fileName, contentType, "logos");

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string directory)
        {
            var safeDirectory = directory.Trim().Trim('/');
            var objectName = $"{safeDirectory}/{Guid.NewGuid()}_{fileName}";

            await GetStorageClient().UploadObjectAsync(_bucketName, objectName, contentType, fileStream);

            return $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{Uri.EscapeDataString(objectName)}?alt=media";
        }

        private StorageClient GetStorageClient()
        {
            if (_storageClient != null)
                return _storageClient;

            try
            {
                if (_credentialPath != null && File.Exists(_credentialPath))
                {
                    var credential = GoogleCredential.FromFile(_credentialPath);
                    _storageClient = StorageClient.Create(credential);
                }
                else
                {
                    _storageClient = StorageClient.Create();
                }

                return _storageClient;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Storage de imagens não configurado para upload.", ex);
            }
        }
    }
}
