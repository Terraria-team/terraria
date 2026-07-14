using System.Security.Cryptography;
using System.Text;
using Lobby.Infrastructure.Services;

namespace Lobby.Tests.Services;

/// <summary>
/// Юніт-тести для <see cref="TokenService"/>.
/// Перевіряють генерацію випадкових токенів (довжина, унікальність)
/// та хешування (детермінізм, різні входи → різні хеші, коректний SHA-256).
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _sut = new();

    // GenerateRandomToken повертає Base64 з 64 випадкових байтів.
    [Fact]
    public void GenerateRandomToken_ReturnsBase64Of64Bytes()
    {
        var token = _sut.GenerateRandomToken();

        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length);
    }

    // Два послідовні виклики дають різні токени (випадковість).
    [Fact]
    public void GenerateRandomToken_ProducesUniqueValues()
    {
        var first = _sut.GenerateRandomToken();
        var second = _sut.GenerateRandomToken();

        Assert.NotEqual(first, second);
    }

    // HashToken детермінований: однаковий вхід → однаковий хеш.
    [Fact]
    public void HashToken_IsDeterministicForSameInput()
    {
        var first = _sut.HashToken("my-token");
        var second = _sut.HashToken("my-token");

        Assert.Equal(first, second);
    }

    // Різні входи дають різні хеші.
    [Fact]
    public void HashToken_ProducesDifferentHashesForDifferentInputs()
    {
        var first = _sut.HashToken("token-a");
        var second = _sut.HashToken("token-b");

        Assert.NotEqual(first, second);
    }

    // Хеш збігається з еталонним SHA-256 у Base64.
    [Fact]
    public void HashToken_MatchesSha256Base64()
    {
        const string token = "my-token";
        var expected = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        var actual = _sut.HashToken(token);

        Assert.Equal(expected, actual);
    }
}
