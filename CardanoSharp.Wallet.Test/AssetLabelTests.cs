using Xunit;
using CardanoSharp.Wallet.Utilities;

namespace CardanoSharp.Wallet.Test
{
    public class AssetLabelTest
    {
        [Fact]
        public void GetAssetLabelHexTest()
        {
            Assert.Equal("00000000", AssetLabelUtility.GetAssetLabelHex(0));
            Assert.Equal("00001070", AssetLabelUtility.GetAssetLabelHex(1));
            Assert.Equal("00017650", AssetLabelUtility.GetAssetLabelHex(23));
            Assert.Equal("000632e0", AssetLabelUtility.GetAssetLabelHex(99));
            Assert.Equal("000643b0", AssetLabelUtility.GetAssetLabelHex(100));
            Assert.Equal("000de140", AssetLabelUtility.GetAssetLabelHex(222));
            Assert.Equal("0014df10", AssetLabelUtility.GetAssetLabelHex(333));
            Assert.Equal("001f4d70", AssetLabelUtility.GetAssetLabelHex(500));
            Assert.Equal("00258a50", AssetLabelUtility.GetAssetLabelHex(600));
            Assert.Equal("00215410", AssetLabelUtility.GetAssetLabelHex(533));
            Assert.Equal("007d0550", AssetLabelUtility.GetAssetLabelHex(2000));
            Assert.Equal("011d7690", AssetLabelUtility.GetAssetLabelHex(4567));
            Assert.Equal("02b670b0", AssetLabelUtility.GetAssetLabelHex(11111));
            Assert.Equal("0c0b0f40", AssetLabelUtility.GetAssetLabelHex(49328));
            Assert.Equal("0ffff240", AssetLabelUtility.GetAssetLabelHex(65535));
        }

        [Fact]
        public void GetAssetLabelIntTest()
        {
            Assert.Equal(0, AssetLabelUtility.GetAssetLabelInt("00000000"));
            Assert.Equal(1, AssetLabelUtility.GetAssetLabelInt("00001070"));
            Assert.Equal(23, AssetLabelUtility.GetAssetLabelInt("00017650"));
            Assert.Equal(99, AssetLabelUtility.GetAssetLabelInt("000632e0"));
            Assert.Equal(100, AssetLabelUtility.GetAssetLabelInt("000643b0"));
            Assert.Equal(222, AssetLabelUtility.GetAssetLabelInt("000de140"));
            Assert.Equal(333, AssetLabelUtility.GetAssetLabelInt("0014df10"));
            Assert.Equal(500, AssetLabelUtility.GetAssetLabelInt("001f4d70"));
            Assert.Equal(600, AssetLabelUtility.GetAssetLabelInt("00258a50"));
            Assert.Equal(533, AssetLabelUtility.GetAssetLabelInt("00215410"));
            Assert.Equal(2000, AssetLabelUtility.GetAssetLabelInt("007d0550"));
            Assert.Equal(4567, AssetLabelUtility.GetAssetLabelInt("011d7690"));
            Assert.Equal(11111, AssetLabelUtility.GetAssetLabelInt("02b670b0"));
            Assert.Equal(49328, AssetLabelUtility.GetAssetLabelInt("0c0b0f40"));
            Assert.Equal(65535, AssetLabelUtility.GetAssetLabelInt("0ffff240"));
        }
    }
}
