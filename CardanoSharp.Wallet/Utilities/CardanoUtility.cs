using System;

public static class CardanoUtility
{
    public static long adaToLovelace = 1000000;
    public static double lovelaceToAda = 0.000001;
    public static long adaOnlyMinUtxo = 1000000;

    // Protocol Parameters
    public static double priceMem = 0.0577;
    public static double priceStep = 0.0000721;
    public static long maxTxExMem = 14000000;
    public static long maxTxExSteps = 10000000000;

    // Slot height at the start of the Shelly Era (Where 1 slot becomes 1 second)
    public static long ShellySlot = 4924800;

    // UNIX time at the start of the Shelly Era
    public static long ShellyUNIX = 1596491091;

    public static long GetSlotFromUnixTIme(long unixTime)
    {
        return unixTime - ShellyUNIX + ShellySlot;
    }

    public static DateTime GetUTCTimeFromSlot(long slot)
    {
        long unixTime = ShellyUNIX + (slot - ShellySlot);
        DateTime posixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime unixDatetime = posixEpoch.AddSeconds(unixTime);
        return unixDatetime;
    }
}
