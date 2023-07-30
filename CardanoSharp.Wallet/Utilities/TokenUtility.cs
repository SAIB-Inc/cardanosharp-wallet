using System.Collections.Generic;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Models.Transactions;

namespace CardanoSharp.Wallet.Utilities
{
    public static class TokenUtility
    {
        public static Dictionary<string, Dictionary<string, long>> ConvertKeysToHexStrings(Dictionary<byte[], NativeAsset> original)
        {
            var result = new Dictionary<string, Dictionary<string, long>>();

            foreach (var pair in original)
            {
                // Convert the outer byte array to a hex string
                string outerHex = pair.Key.ToStringHex();

                // Create a new inner dictionary to hold the converted tokens
                var newInnerDict = new Dictionary<string, long>();

                foreach (var tokenPair in pair.Value.Token)
                {
                    // Convert the inner byte array to a hex string
                    string innerHex = tokenPair.Key.ToStringHex();

                    // Add the converted token to the new inner dictionary
                    newInnerDict[innerHex] = tokenPair.Value;
                }

                // Add the new inner dictionary to the result dictionary
                result[outerHex] = newInnerDict;
            }
            return result;
        }

        public static Dictionary<string, Dictionary<string, long>> MergeStringDictionaries(
            Dictionary<string, Dictionary<string, long>> dictOne,
            Dictionary<string, Dictionary<string, long>> dictTwo
        )
        {
            // Check if the input dictionaries are null
            if (dictOne == null && dictTwo == null)
                return new Dictionary<string, Dictionary<string, long>>();

            if (dictOne == null)
                return new Dictionary<string, Dictionary<string, long>>(dictTwo);

            if (dictTwo == null)
                return new Dictionary<string, Dictionary<string, long>>(dictOne);

            var result = new Dictionary<string, Dictionary<string, long>>(dictOne);
            foreach (var outerPair in dictTwo)
            {
                var outerKey = outerPair.Key;
                var innerDict = outerPair.Value;

                if (!result.TryGetValue(outerKey, out var existingInnerDict))
                    result[outerKey] = new Dictionary<string, long>(innerDict);
                else
                {
                    foreach (var innerPair in innerDict)
                    {
                        var innerKey = innerPair.Key;
                        var value = innerPair.Value;

                        if (existingInnerDict.TryGetValue(innerKey, out var existingValue))
                            existingInnerDict[innerKey] = existingValue + value;
                        else
                            existingInnerDict[innerKey] = value;
                    }
                }
            }

            return result;
        }

        public static Dictionary<byte[], NativeAsset> ConvertStringKeysToByteArrays(Dictionary<string, Dictionary<string, long>> original)
        {
            var result = new Dictionary<byte[], NativeAsset>();
            foreach (var outerPair in original)
            {
                // Convert the outer string to a byte array
                byte[] outerBytes = outerPair.Key.HexToByteArray();

                // Create a new NativeAsset
                var newAsset = new NativeAsset();

                foreach (var innerPair in outerPair.Value)
                {
                    // Convert the inner string to a byte array
                    byte[] innerBytes = innerPair.Key.HexToByteArray();

                    // Add the converted byte array and value to the NativeAsset
                    newAsset.Token[innerBytes] = innerPair.Value;
                }

                // Add the new NativeAsset to the result dictionary
                result[outerBytes] = newAsset;
            }
            return result;
        }
    }
}
