using System;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Common;

namespace CardanoSharp.Wallet.Utilities;

public static class SlotUtility
{
    public static SlotNetworkConfig Mainnet { get; set; } = new SlotNetworkConfig(1596059091000, 4492800, 1000); // Starting at Shelly Era
    public static SlotNetworkConfig Preprod { get; set; } = new SlotNetworkConfig(1654041600000 + 1728000000, 86400, 1000); // Starting at Shelly Era
    public static SlotNetworkConfig Preview { get; set; } = new SlotNetworkConfig(1666656000000, 0, 1000); // Starting at Shelly Era

    public static SlotNetworkConfig GetSlotNetworkConfig(NetworkType networkType)
    {
        if (networkType == NetworkType.Mainnet)
            return Mainnet;
        else if (networkType == NetworkType.Preprod)
            return Preprod;
        else if (networkType == NetworkType.Preview)
            return Preview;

        return new SlotNetworkConfig();
    }

    public static long GetSlotFromUnixTime(SlotNetworkConfig config, long unixTime)
    {
        return unixTime - (config.ZeroTime / 1000) + config.ZeroSlot;
    }

    public static long GetPosixTimeFromSlot(SlotNetworkConfig config, long slot)
    {
        long unixTime = (config.ZeroTime / 1000) + (slot - config.ZeroSlot);
        return unixTime;
    }

    public static DateTime GetUTCTimeFromSlot(SlotNetworkConfig config, long slot)
    {
        long unixTime = GetPosixTimeFromSlot(config, slot);
        DateTime posixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime unixDatetime = posixEpoch.AddSeconds(unixTime);
        return unixDatetime;
    }
}
