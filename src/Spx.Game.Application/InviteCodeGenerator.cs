using System.Security.Cryptography;

namespace Spx.Game.Application;

internal static class InviteCodeGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 6;
    private const ulong SpaceSize = 2_176_782_336UL;

    public static string Generate()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt64(bytes) % SpaceSize;
        return CreateCode(value);
    }

    public static string NormalizeInviteCode(string inviteCode) =>
        inviteCode.Trim().ToUpperInvariant();

    public static string CreateCode(ulong value)
    {
        Span<char> chars = stackalloc char[CodeLength];

        for (var index = CodeLength - 1; index >= 0; index--)
        {
            chars[index] = Alphabet[(int)(value % (ulong)Alphabet.Length)];
            value /= (ulong)Alphabet.Length;
        }

        return new string(chars);
    }
}
