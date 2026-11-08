using TMPro;

/// <summary>TMP metnine GC'siz sayı yazma yardımcıları.</summary>
public static class UIText
{
    /// <summary>prefix + BigNumber(value) + suffix. buffer en az 32 karakter olmalı.</summary>
    public static void SetNumber(TMP_Text label, char[] buffer, double value, string prefix = null, string suffix = null)
    {
        if (label == null || buffer == null) return;
        int len = 0;
        if (prefix != null) for (int i = 0; i < prefix.Length && len < 8; i++) buffer[len++] = prefix[i];
        len += BigNumber.Write(value, buffer, len);
        if (suffix != null) for (int i = 0; i < suffix.Length && len < buffer.Length; i++) buffer[len++] = suffix[i];
        label.SetCharArray(buffer, 0, len);
    }

    /// <summary>"12:34" veya "1:02:03" biçiminde süre.</summary>
    public static void SetDuration(TMP_Text label, char[] buffer, double seconds)
    {
        if (label == null || buffer == null) return;
        long s = seconds <= 0 ? 0 : (long)seconds;
        long h = s / 3600, m = (s / 60) % 60, sec = s % 60;
        int len = 0;
        if (h > 0) { len = WriteInt(h, buffer, len); buffer[len++] = ':'; buffer[len++] = (char)('0' + m / 10); buffer[len++] = (char)('0' + m % 10); }
        else len = WriteInt(m, buffer, len);
        buffer[len++] = ':';
        buffer[len++] = (char)('0' + sec / 10);
        buffer[len++] = (char)('0' + sec % 10);
        label.SetCharArray(buffer, 0, len);
    }

    /// <summary>"3 / 10" ilerleme metni.</summary>
    public static void SetProgress(TMP_Text label, char[] buffer, long current, long target)
    {
        if (label == null || buffer == null) return;
        int len = BigNumber.Write(current < target ? current : target, buffer, 0);
        buffer[len++] = ' '; buffer[len++] = '/'; buffer[len++] = ' ';
        len += BigNumber.Write(target, buffer, len);
        label.SetCharArray(buffer, 0, len);
    }

    private static int WriteInt(long v, char[] buf, int pos)
    {
        if (v == 0) { buf[pos++] = '0'; return pos; }
        int start = pos;
        while (v > 0) { buf[pos++] = (char)('0' + v % 10); v /= 10; }
        System.Array.Reverse(buf, start, pos - start);
        return pos;
    }
}
