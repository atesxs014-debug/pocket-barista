using System;

/// <summary>
/// Idle oyun sayı biçimi: 999 → "999", 1234 → "1.23K", 5_600_000 → "5.60M", sonra B, T, aa, ab ... zz.
/// Önceden ayrılmış char[] içine yazar: string üretmez (GC yok). TMP_Text.SetCharArray ile kullanılır.
/// </summary>
public static class BigNumber
{
    private static readonly string[] Suffixes = BuildSuffixes();

    // "", K, M, B, T, aa, ab, ... zz
    private static string[] BuildSuffixes()
    {
        var result = new string[5 + 26 * 26];
        result[0] = ""; result[1] = "K"; result[2] = "M"; result[3] = "B"; result[4] = "T";
        int k = 5;
        for (char a = 'a'; a <= 'z'; a++)
            for (char b = 'a'; b <= 'z'; b++)
                result[k++] = new string(new[] { a, b });
        return result;
    }

    /// <summary>Değeri buffer'a yazar, yazılan uzunluğu döndürür. Buffer en az 16 karakter olmalı.</summary>
    public static int Write(double value, char[] buffer, int start = 0)
    {
        if (buffer == null || buffer.Length - start < 16) throw new ArgumentException("Buffer en az 16 karakter olmalı");

        int pos = start;
        if (double.IsNaN(value) || double.IsInfinity(value)) { buffer[pos++] = '∞'; return pos - start; }
        if (value < 0) { buffer[pos++] = '-'; value = -value; }

        if (value < 1000)
        {
            pos = WriteInt((long)Math.Floor(value), buffer, pos);
            return pos - start;
        }

        int tier = 0;
        while (value >= 1000 && tier < Suffixes.Length - 1)
        {
            value /= 1000;
            tier++;
        }

        // 3 anlamlı basamak: 1.23K, 12.3K, 123K
        int decimals = value < 10 ? 2 : value < 100 ? 1 : 0;
        double scale = decimals == 2 ? 100 : decimals == 1 ? 10 : 1;
        long scaled = (long)Math.Floor(value * scale);
        long whole = scaled / (long)scale;
        long frac = scaled % (long)scale;

        pos = WriteInt(whole, buffer, pos);
        if (decimals > 0)
        {
            buffer[pos++] = '.';
            if (decimals == 2 && frac < 10) buffer[pos++] = '0';
            pos = WriteInt(frac, buffer, pos);
        }

        string suffix = Suffixes[tier];
        for (int i = 0; i < suffix.Length; i++) buffer[pos++] = suffix[i];
        return pos - start;
    }

    /// <summary>UI dışı yerler için (log, nadir metin). Allocation yapar.</summary>
    public static string Format(double value)
    {
        var buf = new char[24];
        int len = Write(value, buf);
        return new string(buf, 0, len);
    }

    private static int WriteInt(long value, char[] buf, int pos)
    {
        if (value == 0) { buf[pos++] = '0'; return pos; }
        int digits = 0;
        for (long v = value; v > 0; v /= 10) digits++;
        int end = pos + digits;
        for (int i = end - 1; i >= pos; i--) { buf[i] = (char)('0' + (int)(value % 10)); value /= 10; }
        return end;
    }
}
