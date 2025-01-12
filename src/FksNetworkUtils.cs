namespace FksNetworking;

public class FksNetworkUtils
{
    public static int CalculateTickrateMs(uint tickrate)
    {
        return (int)(1f / tickrate) * 1000;
    }
}
