namespace Lobby.DTOs;

public record GoogleLoginRecord(string Code, string RedirectUri);
public record RefreshTokenRecord(string RefreshToken);