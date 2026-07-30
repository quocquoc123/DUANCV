using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace QLBanDoAnNhanh.Services
{
    /// <summary>
    /// Service upload/xóa ảnh trên Cloudinary, có fallback lưu local wwwroot khi chưa cấu hình Cloudinary.
    /// </summary>
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CloudinaryService> _logger;
        private readonly bool _isConfigured;

        public CloudinaryService(
            IConfiguration configuration,
            IWebHostEnvironment env,
            ILogger<CloudinaryService> logger)
        {
            _env = env;
            _logger = logger;

            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            _isConfigured = IsRealCredential(cloudName)
                && IsRealCredential(apiKey)
                && IsRealCredential(apiSecret);

            if (_isConfigured)
            {
                var account = new Account(cloudName, apiKey, apiSecret);
                _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
            }
            else
            {
                _cloudinary = null;
                _logger.LogWarning(
                    "Cloudinary chưa cấu hình (CloudName/ApiKey/ApiSecret). Ảnh sẽ được lưu local tại wwwroot/images.");
            }
        }

        /// <summary>
        /// Upload ảnh lên Cloudinary (nếu đã cấu hình) hoặc lưu local wwwroot/images/{folder}.
        /// </summary>
        public async Task<string> UploadImageAsync(IFormFile file, string folder = "chinhanh")
        {
            if (file == null || file.Length == 0)
                return null;

            if (_isConfigured && _cloudinary != null)
            {
                try
                {
                    await using var stream = file.OpenReadStream();
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = folder,
                        Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK
                        && uploadResult.SecureUrl != null)
                    {
                        return uploadResult.SecureUrl.ToString();
                    }

                    _logger.LogWarning(
                        "Cloudinary upload thất bại: {Error}. Chuyển sang lưu local.",
                        uploadResult.Error?.Message ?? uploadResult.StatusCode.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cloudinary upload exception. Chuyển sang lưu local.");
                }
            }

            return await SaveLocalAsync(file, folder);
        }

        /// <summary>
        /// Xóa ảnh khỏi Cloudinary theo publicId (bỏ qua nếu là ảnh local).
        /// </summary>
        public async Task DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId)) return;
            if (!_isConfigured || _cloudinary == null) return;
            if (publicId.StartsWith("images/", StringComparison.OrdinalIgnoreCase)
                || publicId.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                var deleteParams = new DeletionParams(publicId);
                await _cloudinary.DestroyAsync(deleteParams);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Xóa ảnh Cloudinary thất bại: {PublicId}", publicId);
            }
        }

        /// <summary>
        /// Trích xuất publicId từ URL Cloudinary, hoặc trả về path local nếu là /images/...
        /// </summary>
        public static string ExtractPublicId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            try
            {
                if (url.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
                    || url.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
                {
                    return url.TrimStart('/');
                }

                var uploadIdx = url.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
                if (uploadIdx < 0) return null;

                var afterUpload = url[(uploadIdx + 8)..];
                if (afterUpload.StartsWith("v") && afterUpload.Contains("/"))
                {
                    afterUpload = afterUpload[(afterUpload.IndexOf('/') + 1)..];
                }

                var dotIdx = afterUpload.LastIndexOf('.');
                return dotIdx >= 0 ? afterUpload[..dotIdx] : afterUpload;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Xóa file local nếu URL thuộc wwwroot/images.
        /// </summary>
        public void DeleteLocalIfExists(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!url.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_env.WebRootPath, relative);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không xóa được ảnh local: {Url}", url);
            }
        }

        private async Task<string> SaveLocalAsync(IFormFile file, string folder)
        {
            var safeFolder = string.IsNullOrWhiteSpace(folder)
                ? "uploads"
                : string.Concat(folder.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));

            var uploadDir = Path.Combine(_env.WebRootPath, "images", safeFolder);
            Directory.CreateDirectory(uploadDir);

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext))
                ext = ".jpg";

            var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var fullPath = Path.Combine(uploadDir, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/images/{safeFolder}/{fileName}";
        }

        private static bool IsRealCredential(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim();
            return !v.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(v, "CHANGE_ME", StringComparison.OrdinalIgnoreCase);
        }
    }
}
