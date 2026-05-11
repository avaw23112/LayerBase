namespace LayerBase.Actor;

internal static class ActorMailCapacity
{
    public static int NormalizePowerOfTwo(int value)
    {
        if (value <= 1)
        {
            return 1;
        }

        int result = 1;
        while (result < value)
        {
            result <<= 1;
        }

        return result;
    }

    public static int Wrap(int value, int capacity)
    {
        return value & (capacity - 1);
    }
}
