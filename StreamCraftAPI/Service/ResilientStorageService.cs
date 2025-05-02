using StreamCraftAPI.Interface;

namespace StreamCraftAPI.Service
{
    public class ResilientStorageService : IPermanentStorageService
    {
        private readonly S3StorageService _s3;
        private readonly LocalFallbackStorageService _fallback;

        public ResilientStorageService(S3StorageService s3, LocalFallbackStorageService fallback)
        {
            _s3 = s3;
            _fallback = fallback;
        }

        public async Task<string> UploadVideoAsync(string path, string name)
        {
            try { return await _s3.UploadVideoAsync(path, name); }
            catch { return await _fallback.UploadVideoAsync(path, name); }
        }

        public async Task<string> UploadThumbnailAsync(string path, string name)
        {
            try { return await _s3.UploadThumbnailAsync(path, name); }
            catch { return await _fallback.UploadThumbnailAsync(path, name); }
        }
    }

}
