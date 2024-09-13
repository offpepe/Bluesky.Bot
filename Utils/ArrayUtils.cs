namespace bsky.bot.Utils;

public static class ArrayUtils
{
    public static void Push<T>(ref T[] array, T item)
    {
        var lastPos = array.Length;
        Array.Resize(ref array, lastPos + 1);
        array[lastPos] = item;
    }
    public static void PushRange<T>(ref T[] array, T[] src)
    {
        var lastSize = array.Length;
        Array.Resize(ref array, lastSize + src.Length);
        var srcIdx = 0;
        for (var i = lastSize; i < array.Length; i++)
        {
            array[i] = src[srcIdx++];
        }
    }
}