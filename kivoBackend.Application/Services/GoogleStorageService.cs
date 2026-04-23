using Google.Cloud.Storage.V1;
using kivoBackend.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    public class GoogleStorageService : IStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName = "kivo-bucket";

        public GoogleStorageService()
        {
            _storageClient = StorageClient.Create();
        }
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var objectName = $"logos/{Guid.NewGuid()}_{fileName}";

            await _storageClient.UploadObjectAsync(_bucketName, objectName, contentType, fileStream);

            return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
        }
    }
}
