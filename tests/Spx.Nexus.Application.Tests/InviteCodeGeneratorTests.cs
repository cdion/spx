using Spx.Nexus.Application;
using Xunit;

namespace Spx.Nexus.Application.Tests;

public sealed class InviteCodeGeneratorTests
{
    [Theory]
    [InlineData(" abc123 ", "ABC123")]
    [InlineData("xyz789", "XYZ789")]
    public void NormalizeInviteCode_trims_and_uppercases(string input, string expected)
    {
        Assert.Equal(expected, InviteCodeGenerator.NormalizeInviteCode(input));
    }

    [Theory]
    [InlineData(0UL, "AAAAAA")]
    [InlineData(35UL, "AAAAA9")]
    [InlineData(36UL, "AAAABA")]
    public void CreateCode_maps_value_to_expected_base36_code(ulong value, string expected)
    {
        Assert.Equal(expected, InviteCodeGenerator.CreateCode(value));
    }

    [Fact]
    public void Generate_returns_six_character_uppercase_alpha_numeric_code()
    {
        var code = InviteCodeGenerator.Generate();

        Assert.Matches("^[A-Z0-9]{6}$", code);
    }
}
