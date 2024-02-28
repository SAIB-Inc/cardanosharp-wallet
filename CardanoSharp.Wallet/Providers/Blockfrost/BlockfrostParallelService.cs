using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CardanoSharp.Blockfrost.Sdk.Contracts;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.Utilities;

namespace CardanoSharp.Wallet.Providers.Blockfrost;

public partial class BlockfrostService
{
    //---------------------------------------------------------------------------------------------------//
    // Account Functions
    //---------------------------------------------------------------------------------------------------//
    public async Task<List<Asset>> GetAccountAssetsParallelized(string mainAddress)
    {
        try
        {
            Address addr = new(mainAddress);
            Address stakeAddr = addr.GetStakeAddress();
            string stakeAddress = stakeAddr.ToString();

            int countPerPage = 100;
            string order = "desc";
            int batchCount = 4;
            int initialPage = 1;

            List<Asset> allAssets = new();
            while (true)
            {
                // Prepare a batch of tasks
                var batchTasks = new List<Task<List<Asset>>>();
                for (int i = 0; i < batchCount; i++)
                {
                    int pageNumber = initialPage + i;
                    batchTasks.Add(GetAccountAssetsHelper(stakeAddress, countPerPage, pageNumber, order));
                }

                // Fetch the batch
                var batchResults = await Task.WhenAll(batchTasks);
                allAssets.AddRange(batchResults.SelectMany(x => x));

                // If any page in the batch has less than countPerPage, we've reached the last page
                if (batchResults.Any(x => x.Count < countPerPage))
                    break;
                else
                    initialPage += batchCount;
            }

            return allAssets;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.ToString());
            return new List<Asset>();
        }
    }

    private async Task<List<Asset>> GetAccountAssetsHelper(string stakeAddress, int countPerPage, int pageNumber, string order = "desc")
    {
        List<Asset> assets = new();
        var addressAssets = (await AccountClient.GetAccountAssociatedAddressesAssets(stakeAddress, countPerPage, pageNumber, order))?.Content!;
        foreach (var addressAsset in addressAssets)
        {
            Asset asset =
                new()
                {
                    PolicyId = AssetUtility.GetHexPolicyId(addressAsset.Unit),
                    Name = AssetUtility.GetHexAssetName(addressAsset.Unit),
                    Quantity = long.Parse(addressAsset.Quantity)
                };
            assets.Add(asset);
        }

        return assets;
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Address Functions
    //---------------------------------------------------------------------------------------------------//
    public async Task<List<string>> GetAddressesParallelized(string addressParam)
    {
        List<string> addresses = new();
        List<string> initialList = await GetAddressesHelper(addressParam, 1);

        // Check if there are 100 or less addresses first to handle low address wallets quickly
        if (initialList.Count < 100)
        {
            foreach (string address in initialList)
                addresses.Add(address);
        }
        else
        {
            int loopAddressCount = 0;
            int itemsPerPage = 100;

            //Get's 500 addresses at once, can sometimes throw strange results in local build,
            //if this occurs in staging and production use, will be reverted back to old method.
            var data = new int[] { 1, 2, 3, 4, 5 };
            do
            {
                loopAddressCount = 0;
                var result = new ConcurrentBag<List<string>>();
                var tasks = new List<Task>();
                Parallel.ForEach(
                    data,
                    (pageNumber) =>
                    {
                        tasks.Add(CallGetAddressesHelperAsync(addressParam, pageNumber, result));
                    }
                );

                await Task.WhenAll(tasks);
                foreach (List<string> stringList in result)
                {
                    foreach (string address in stringList)
                    {
                        loopAddressCount += 1;
                        addresses.Add(address);
                    }
                }

                // Increment all data items by the data length to get new items from new pages
                for (int i = 0; i < data.Length; i++)
                    data[i] = data[i] + data.Length;
            } while (loopAddressCount == data.Length * itemsPerPage);
        }
        return addresses;
    }

    private async Task<List<string>> GetAddressesHelper(string address, int pageNumber = 1, string order = "asc")
    {
        List<string> addresses = new();
        try
        {
            Address addr = new(address);
            Address stakeAddr = addr.GetStakeAddress();
            string stakeAddress = stakeAddr.ToString();

            int countPerPage = 100;
            var blockfrostAddresses = await AccountClient.GetAccountAssociatedAddresses(stakeAddress, countPerPage, pageNumber, order);
            if (blockfrostAddresses.Content == null)
                return addresses;

            foreach (var blockfrostAddress in blockfrostAddresses.Content)
                addresses.Add(blockfrostAddress.Address);
            return addresses;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.ToString());
            return addresses;
        }
    }

    private async Task CallGetAddressesHelperAsync(string address, int pageNumber, ConcurrentBag<List<string>> results)
    {
        List<string> addresses = await GetAddressesHelper(address, pageNumber);
        results.Add(addresses);
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Utxo Functions
    //---------------------------------------------------------------------------------------------------//
    public async Task<List<Utxo>> GetUtxosParallelized(string address, bool filterSmartContractAddresses = false)
    {
        List<Utxo> utxos = new();
        List<string> addresses = await GetAddressesParallelized(address);
        if (filterSmartContractAddresses)
            addresses = AddressUtility.FilterSmartContractAddresses(addresses);

        int maxAddressCount = 1200;
        if (addresses.Count < maxAddressCount)
        {
            List<string> totalCheckedAddresses = new(); //The addresses that you have done + the addresses you are about to do.
            List<string> loopAddresses = new(); //the addresses you are about to do.
            int addressesPerLoop = 300;
            do
            {
                loopAddresses.Clear();
                int addressCheckCount = addresses.Count - totalCheckedAddresses.Count;
                if (addressCheckCount >= addressesPerLoop)
                    addressCheckCount = addressesPerLoop;

                int totalAddressCount = totalCheckedAddresses.Count;
                for (int i = totalAddressCount; i < (addressCheckCount + totalAddressCount); i++)
                {
                    totalCheckedAddresses.Add(addresses[i]);
                    loopAddresses.Add(addresses[i]);
                }

                var resultUTXO = new ConcurrentBag<List<Utxo>>();

                var tasksUTXOS = new List<Task>();
                Parallel.ForEach(
                    loopAddresses,
                    (address) =>
                    {
                        tasksUTXOS.Add(CallGetUtxosHelperAsync(address, resultUTXO));
                    }
                );

                var cutTasksUTXOS = new List<Task>();
                for (int i = 0; i < tasksUTXOS.Count; i++)
                {
                    if (tasksUTXOS[i] != null)
                    {
                        cutTasksUTXOS.Add(tasksUTXOS[i]);
                    }
                }

                await Task.WhenAll(cutTasksUTXOS);

                foreach (List<Utxo> utxoList in resultUTXO)
                {
                    foreach (Utxo utxo in utxoList)
                    {
                        utxos.Add(utxo);
                    }
                }
            } while (addresses.Count != totalCheckedAddresses.Count);
        }
        else
        {
            // If there are an extreme number of addresses, just check the first 100 and last 100
            List<string> firstAndLast100Addresses = new();
            for (int i = 0; i < 100; i++)
                firstAndLast100Addresses.Add(addresses[i]);

            for (int i = (addresses.Count - 1); i >= (addresses.Count - 100); i--)
                firstAndLast100Addresses.Add(addresses[i]);

            var resultUTXO = new ConcurrentBag<List<Utxo>>();
            var tasksUTXOS = new List<Task>();
            Parallel.ForEach(
                firstAndLast100Addresses,
                (address) =>
                {
                    tasksUTXOS.Add(CallGetUtxosHelperAsync(address, resultUTXO));
                }
            );

            var cutTasksUTXOS = new List<Task>();
            for (int i = 0; i < tasksUTXOS.Count; i++)
            {
                if (tasksUTXOS[i] != null)
                {
                    cutTasksUTXOS.Add(tasksUTXOS[i]);
                }
            }

            await Task.WhenAll(cutTasksUTXOS);

            foreach (List<Utxo> utxoList in resultUTXO)
            {
                foreach (Utxo utxo in utxoList)
                {
                    utxos.Add(utxo);
                }
            }
        }
        return utxos;
    }

    private async Task<List<Utxo>> GetUtxosHelper(string address)
    {
        List<Utxo> utxos = new();
        try
        {
            int pageNumber = 1;
            int countPerPage = 100;
            string order = "desc";
            AddressUtxo[]? blockfrostAddressUtxos;
            do
            {
                blockfrostAddressUtxos = (await AddressesClient.GetAddressUtxosAsync(address, countPerPage, pageNumber, order))?.Content;
                if (blockfrostAddressUtxos == null)
                    break;

                foreach (var blockfrostAddressUtxo in blockfrostAddressUtxos)
                {
                    string txHash = blockfrostAddressUtxo.TxHash!;
                    uint txIndex = blockfrostAddressUtxo.OutputIndex;
                    var amountObjects = JsonSerializer.Serialize(blockfrostAddressUtxo.Amount);
                    List<Amount> amounts = JsonSerializer.Deserialize<List<Amount>>(amountObjects)!;
                    Balance balance = ValueUtility.GetBalance(amounts);

                    string outputAddress = blockfrostAddressUtxo.Address!;
                    DatumOption? datumOption =
                        blockfrostAddressUtxo.InlineDatum != null
                            ? DatumOptionExtension.DeserializeFromInlineDatum(blockfrostAddressUtxo.InlineDatum!.HexToByteArray())
                            : null;

                    Utxo utxo =
                        new()
                        {
                            TxHash = txHash,
                            TxIndex = txIndex,
                            Balance = balance,
                            OutputAddress = outputAddress,
                            OutputDatumOption = datumOption,
                        };
                    utxos.Add(utxo);
                }
                pageNumber += 1;
            } while (blockfrostAddressUtxos?.Length == countPerPage);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.ToString());
        }
        return utxos;
    }

    private async Task CallGetUtxosHelperAsync(string address, ConcurrentBag<List<Utxo>> results)
    {
        var utxos = await GetUtxosHelper(address);
        results.Add(utxos);
    }
    //---------------------------------------------------------------------------------------------------//
}
