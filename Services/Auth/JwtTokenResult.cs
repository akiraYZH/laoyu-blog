namespace laoyu_blog_backend.Services.Auth;

public sealed record JwtTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc
);