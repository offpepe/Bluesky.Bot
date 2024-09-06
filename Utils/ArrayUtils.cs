namespace bsky.bot.Utils;

public static class ArrayUtils
{
    public static void Push<T>(ref T[] array, T item)
    {
        var lastPos = array.Length;
        Array.Resize(ref array, lastPos + 1);
        array[lastPos] = item;
    }
}