using System;
using System.Text;

public static class GameCodeGenerator
{
    private static readonly char[] charset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();
    private static readonly Random rng = new Random();

    public static string Generate(int sections = 2, int length = 4, char separator = '-')
    {
        var sb = new StringBuilder();
        for (int s = 0; s < sections; s++)
        {
            for (int i = 0; i < length; i++)
                sb.Append(charset[rng.Next(charset.Length)]);
            if (s < sections - 1) sb.Append(separator);
        }
        return sb.ToString();
    }
}
