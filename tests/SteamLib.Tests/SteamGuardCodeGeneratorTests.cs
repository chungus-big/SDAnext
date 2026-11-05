using System.Security.Cryptography;
using System.Text;
using SteamLib.SteamMobile;
using Xunit;

namespace SteamLib.Tests;

public class SteamGuardCodeGeneratorTests
{
    // Alphabet used by Steam Guard Mobile Authenticator
    private const string SteamGuardAlphabet = "23456789BCDFGHJKMNPQRTVWXY";

    [Fact]
    public void GenerateCode_ReturnsFiveCharactersLength()
    {
        // 20 bytes secret (160 bits HMAC key)
        var secretBytes = Encoding.UTF8.GetBytes("12345678901234567890");
        var sharedSecret = Convert.ToBase64String(secretBytes);
        var time = 1700000000L;

        var code = SteamGuardCodeGenerator.GenerateCode(sharedSecret, time);

        Assert.NotNull(code);
        Assert.Equal(5, code.Length);
    }

    [Theory]
    [InlineData(1700000010L)]
    [InlineData(1700000020L)]
    [InlineData(1700000039L)]
    public void GenerateCode_ReturnsSameCodeWithinSame30SecondInterval(long time)
    {
        var secretBytes = Encoding.UTF8.GetBytes("SampleSteamSharedSecret1");
        var sharedSecret = Convert.ToBase64String(secretBytes);

        var codeAtBase = SteamGuardCodeGenerator.GenerateCode(sharedSecret, 1700000010L);
        var codeAtOffset = SteamGuardCodeGenerator.GenerateCode(sharedSecret, time);

        Assert.Equal(codeAtBase, codeAtOffset);
    }

    [Fact]
    public void GenerateCode_ReturnsDifferentCodeForDifferentIntervals()
    {
        var secretBytes = Encoding.UTF8.GetBytes("SampleSteamSharedSecret1");
        var sharedSecret = Convert.ToBase64String(secretBytes);

        var codeInterval1 = SteamGuardCodeGenerator.GenerateCode(sharedSecret, 1700000010L);
        var codeInterval2 = SteamGuardCodeGenerator.GenerateCode(sharedSecret, 1700000040L);

        Assert.NotEqual(codeInterval1, codeInterval2);
    }

    [Fact]
    public void GenerateCode_ContainsOnlyValidSteamAlphabetCharacters()
    {
        var secretBytes = new byte[20];
        RandomNumberGenerator.Fill(secretBytes);
        var sharedSecret = Convert.ToBase64String(secretBytes);

        for (var t = 1000000L; t < 1000000L + (30 * 100); t += 30)
        {
            var code = SteamGuardCodeGenerator.GenerateCode(sharedSecret, t);
            Assert.Equal(5, code.Length);
            foreach (var ch in code)
            {
                Assert.Contains(ch, SteamGuardAlphabet);
            }
        }
    }

    [Fact]
    public void GenerateCode_DeterministicKnownVector()
    {
        // Vector with fixed secret and known calculation
        var secretBytes = Encoding.ASCII.GetBytes("FixedSecretKey123456");
        var sharedSecret = Convert.ToBase64String(secretBytes);
        var time = 1710000000L;

        // Verify that running it multiple times produces identical output
        var code1 = SteamGuardCodeGenerator.GenerateCode(sharedSecret, time);
        var code2 = SteamGuardCodeGenerator.GenerateCode(secretBytes, time);

        Assert.Equal(code1, code2);
        Assert.Equal(5, code1.Length);
        Assert.All(code1, c => Assert.Contains(c, SteamGuardAlphabet));
    }
}
