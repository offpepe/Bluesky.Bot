using System.Text;

namespace bsky.bot.Utils;

public static class Utf16ToUtf8Converter
{
    public static int Utf16IndexToUtf8Index(this string str, int index)
    {
        if (index < 0 || index > str.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        var utf8Index = 0;
        for (var i = 0; i < index; i++)
        {
            var c = str[i];
            if (char.IsHighSurrogate(c) && i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]))
            {
                utf8Index += 4;
                i++;
            }
            else if (c <= 0x7F)
            {
                utf8Index += 1;
            }
            else if (c <= 0x7FF)
            {
                utf8Index += 2;
            }
            else
            {
                utf8Index += 3;
            }
        }

        return utf8Index;
    }
}