using Microsoft.AspNetCore.Http;

namespace laoyu_blog_backend.Services;

public interface IImageStorageService
{
    Task<ImageUploadResult> SaveAsync(
        IFormFile file,
        CancellationToken cancellationToken);
}