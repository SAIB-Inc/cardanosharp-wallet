using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CardanoSharp.Blockfrost.Sdk.Contracts;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;

namespace CardanoSharp.Wallet.Providers.Blockfrost;

public partial class BlockfrostService
{
    //---------------------------------------------------------------------------------------------------//
    // Helper Functions
    //---------------------------------------------------------------------------------------------------//
    public async Task<List<string>> GetAddressesHelper(string address, int pageNumber = 1, string order = "asc")
    {
        List<string> addresses = new();
        try
        {
            Address addr = new(address);
            Address stakeAddr = addr.GetStakeAddress();
            string stakeAddress = stakeAddr.ToString();

            int countPerPage = 100;
            var blockfrostAddresses = await _accountClient.GetAccountAssociatedAddresses(stakeAddress, countPerPage, pageNumber, order);
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

    public async Task<List<Utxo>> GetUtxosHelper(string address)
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
                blockfrostAddressUtxos = (await _addressesClient.GetAddressUtxosAsync(address, countPerPage, pageNumber, order))?.Content;
                if (blockfrostAddressUtxos == null)
                    break;

                foreach (var blockfrostAddressUtxo in blockfrostAddressUtxos)
                {
                    string txHash = blockfrostAddressUtxo.TxHash!;
                    uint txIndex = blockfrostAddressUtxo.OutputIndex;
                    var amountObjects = JsonSerializer.Serialize(blockfrostAddressUtxo.Amount);
                    List<Amount> amounts = JsonSerializer.Deserialize<List<Amount>>(amountObjects)!;
                    Balance balance = GetBalance(amounts);

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
    //---------------------------------------------------------------------------------------------------//
}
