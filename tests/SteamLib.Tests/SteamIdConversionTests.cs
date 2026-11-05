using SteamLib.Utility;
using Xunit;

namespace SteamLib.Tests;

public class SteamIdConversionTests
{
    private const long KnownSteam64 = 76561197960287930L;
    private const string KnownSteam2 = "STEAM_0:0:11101";
    private const string KnownSteam3 = "[U:1:22202]";

    [Fact]
    public void FromSteam64_ConvertsToCorrectSteam2AndSteam3()
    {
        var id = SteamId.FromSteam64(KnownSteam64);

        Assert.Equal(KnownSteam64, id.Steam64.Id);
        Assert.Equal(KnownSteam2, id.Steam2.ToString());
        Assert.Equal(KnownSteam3, id.Steam3.ToString());
    }

    [Fact]
    public void SteamIdParser_Parse64_SuccessfullyParsesValidSteam64()
    {
        var id = SteamIdParser.Parse(KnownSteam64.ToString());

        Assert.Equal(KnownSteam64, id.Steam64.Id);
        Assert.Equal(KnownSteam2, id.Steam2.ToString());
        Assert.Equal(KnownSteam3, id.Steam3.ToString());
    }

    [Fact]
    public void SteamIdParser_Parse2_SuccessfullyParsesSteam2()
    {
        var id = SteamIdParser.Parse(KnownSteam2);

        Assert.Equal(KnownSteam64, id.Steam64.Id);
        Assert.Equal(KnownSteam3, id.Steam3.ToString());
        Assert.Equal(0, id.Steam2.Universe);
        Assert.Equal(0, id.Steam2.LowestBit);
        Assert.Equal(11101, id.Steam2.HighestBit);
    }

    [Fact]
    public void SteamIdParser_Parse3_SuccessfullyParsesSteam3()
    {
        var id = SteamIdParser.Parse(KnownSteam3);

        Assert.Equal(KnownSteam64, id.Steam64.Id);
        Assert.Equal(KnownSteam2, id.Steam2.ToString());
        Assert.Equal(22202, id.Steam3.Id);
        Assert.Equal('U', id.Steam3.Type);
    }

    [Fact]
    public void Roundtrip_Conversions_PreserveOriginalValues()
    {
        // Start from Steam64
        var original = SteamId.FromSteam64(KnownSteam64);

        // Convert to string representations
        var s64Str = original.ToString();
        var s2Str = original.Steam2.ToString();
        var s3Str = original.Steam3.ToString();

        // Parse each back into SteamId
        var parsed64 = SteamIdParser.Parse(s64Str);
        var parsed2 = SteamIdParser.Parse(s2Str);
        var parsed3 = SteamIdParser.Parse(s3Str);

        Assert.Equal(original, parsed64);
        Assert.Equal(original, parsed2);
        Assert.Equal(original, parsed3);
    }

    [Theory]
    [InlineData("not_a_steam_id")]
    [InlineData("12345")]
    [InlineData("STEAM_invalid")]
    [InlineData("[X:1:abc]")]
    public void TryParse_ReturnsFalse_ForInvalidInputs(string invalidInput)
    {
        var success = SteamIdParser.TryParse(invalidInput, out _);
        Assert.False(success);
    }

    [Theory]
    [InlineData("invalid_string")]
    [InlineData("7656119")] // below SEED
    public void Parse_ThrowsFormatException_ForInvalidInputs(string invalidInput)
    {
        Assert.Throws<FormatException>(() => SteamIdParser.Parse(invalidInput));
    }

    [Fact]
    public void EqualityAndOperators_WorkAsExpected()
    {
        var id1 = SteamId.FromSteam64(KnownSteam64);
        var id2 = SteamIdParser.Parse(KnownSteam2);
        var id3 = SteamId.FromSteam64(KnownSteam64 + 1);

        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.True(id1 != id3);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }
}
