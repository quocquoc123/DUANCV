using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace QLBanDoAnNhanh.Services
{
    /// <summary>
    /// Service upload/xóa ảnh trên Cloudinary.
    /// </summary>
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey    = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        }

        /// <summary>
        /// Upload ảnh lên Cloudinary và trả về URL bảo mật (https).
        /// </summary>
        /// <param name="file">File ảnh từ IFormFile.</param>
        /// <param name="folder">Thư mục lưu trên Cloudinary (vd: "chinhanh").</param>
        /// <returns>URL ảnh hoặc null nếu thất bại.</returns>
        public async Task<string?> UploadImageAsync(IFormFile file, string folder = "chinhanh")
        {
            if (file == null || file.Length == 0)
                return null;

            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File           = new FileDescription(file.FileName, stream),
                Folder         = folder,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.StatusCode == System.Net.HttpStatusCode.OK
                ? uploadResult.SecureUrl.ToString()
                : null;
        }

        /// <summary>
        /// Xóa ảnh khỏi Cloudinary theo publicId.
        /// </summary>
        /// <param name="publicId">Public ID của ảnh trên Cloudinary.</param>
        public async Task DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId)) return;
            var deleteParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deleteParams);
        }

        /// <summary>
        /// Trích xuất publicId từ URL Cloudinary.
        /// Ví dụ: "https://res.cloudinary.com/demo/image/upload/v123/chinhanh/abc.jpg"
        ///         => "chinhanh/abc"
        /// </summary>
        public static string? ExtractPublicId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            try
            {
                // Tìm "/upload/" và lấy phần sau (bỏ phiên bản v...)
                var uploadIdx = url.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
                if (uploadIdx < 0) return null;

                var afterUpload = url[(uploadIdx + 8)..]; // bỏ "/upload/"
                // Bỏ phần vNNNN/ ở đầu nếu có
                if (afterUpload.StartsWith("v") && afterUpload.Contains("/"))
                {
                    afterUpload = afterUpload[(afterUpload.IndexOf('/') + 1)..];
                }
                // Bỏ phần mở rộng file
                var dotIdx = afterUpload.LastIndexOf('.');
                return dotIdx >= 0 ? afterUpload[..dotIdx] : afterUpload;
            }
            catch { return null; }
        }
    }
}
