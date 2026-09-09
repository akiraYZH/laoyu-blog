namespace laoyu_blog_backend.Dtos.Auth;

public sealed record LoginResponseDto(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc);