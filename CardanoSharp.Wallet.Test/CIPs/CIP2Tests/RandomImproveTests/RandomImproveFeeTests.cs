using System;
using System.Collections.Generic;
using System.Linq;
using CardanoSharp.Wallet.CIPs.CIP2;
using CardanoSharp.Wallet.CIPs.CIP2.ChangeCreationStrategies;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Transactions;
using Xunit;

namespace CardanoSharp.Wallet.Test.CIPs;

public partial class CIP2Tests
{
    [Fact]
    public void RandomImprove_Simple_Fee_Test()
    {
        //arrange
        var coinSelection = new CoinSelectionService(new RandomImproveStrategy(), new SingleTokenBundleStrategy());
        var outputs = new List<TransactionOutput>() { output_100_ada_no_assets };
        var utxos = new List<Utxo>() { utxo_50_ada_no_assets, utxo_50_ada_no_assets, utxo_10_ada_no_assets, utxo_20_ada_no_assets, };

        //act
        ulong feeBuffer = 21 * adaToLovelace;
        var response = coinSelection.GetCoinSelection(outputs, utxos, address, feeBuffer: feeBuffer);

        //assert
        long totalSelected = 0;
        response.SelectedUtxos.ForEach(s => totalSelected = totalSelected + (long)s.Balance.Lovelaces);
        long totalOutput = 0;
        outputs.ForEach(o => totalOutput = totalOutput + (long)o.Value.Coin);
        long totalChange = 0;
        response.ChangeOutputs.ForEach(s => totalChange = totalChange + (long)s.Value.Coin);
        long finalChangeOutputChange = (long)response.ChangeOutputs.Last().Value.Coin;
        Assert.Equal(totalSelected, totalOutput + totalChange);
        Assert.True((ulong)finalChangeOutputChange >= feeBuffer);
    }

    [Fact]
    public void RandomImprove_Simple_Fee_Fail_Test()
    {
        //arrange
        var coinSelection = new CoinSelectionService(new RandomImproveStrategy(), new SingleTokenBundleStrategy());
        var outputs = new List<TransactionOutput>() { output_100_ada_no_assets };
        var utxos = new List<Utxo>() { utxo_50_ada_no_assets, utxo_50_ada_no_assets, utxo_10_ada_no_assets, };

        //assert
        try
        {
            //act
            var response = coinSelection.GetCoinSelection(outputs, utxos, address, feeBuffer: 11 * adaToLovelace);
        }
        catch (Exception e)
        {
            //assert
            Assert.Equal("UTxOs have insufficient balance", e.Message);
        }
    }

    [Fact]
    public void RandomImprove_BasicChange_Fee_Test()
    {
        //arrange
        var coinSelection = new CoinSelectionService(new RandomImproveStrategy(), new BasicChangeSelectionStrategy());
        var outputs = new List<TransactionOutput>() { output_10_ada_no_assets, output_10_ada_no_assets, output_10_ada_no_assets };
        var utxos = new List<Utxo>()
        {
            utxo_10_ada_10_tokens,
            utxo_10_ada_1_owned_mint_asset,
            utxo_10_ada_1_owned_mint_asset_two,
            utxo_10_ada_100_owned_mint_asset,
        };

        //act
        ulong feeBuffer = 3 * adaToLovelace;
        var response = coinSelection.GetCoinSelection(outputs, utxos, address, feeBuffer: feeBuffer);

        //assert
        int selectedUTXOsLength = response.SelectedUtxos.Count;
        int changeOutputsLength = response.ChangeOutputs.Count;
        Assert.Equal(4, selectedUTXOsLength);
        Assert.Equal(1, changeOutputsLength);

        long totalSelected = 0;
        response.SelectedUtxos.ForEach(s => totalSelected = totalSelected + (long)s.Balance.Lovelaces);
        long totalOutput = 0;
        outputs.ForEach(o => totalOutput = totalOutput + (long)o.Value.Coin);
        long totalChange = 0;
        response.ChangeOutputs.ForEach(s => totalChange = totalChange + (long)s.Value.Coin);
        long finalChangeOutputChange = (long)response.ChangeOutputs.Last().Value.Coin;
        Assert.Equal(totalSelected, totalOutput + totalChange);
        Assert.True((ulong)finalChangeOutputChange >= feeBuffer);
    }

    [Fact]
    public void RandomImprove_BasicChange_Fee_Test_2()
    {
        //arrange
        var coinSelection = new CoinSelectionService(new RandomImproveStrategy(), new BasicChangeSelectionStrategy());
        var outputs = new List<TransactionOutput>() { output_10_ada_no_assets, output_10_ada_no_assets, output_10_ada_no_assets };
        var utxos = new List<Utxo>()
        {
            utxo_10_ada_10_tokens,
            utxo_10_ada_1_owned_mint_asset,
            utxo_10_ada_1_owned_mint_asset_two,
            utxo_10_ada_100_owned_mint_asset,
            utxo_10_ada_100_owned_mint_asset_two,
        };

        //act
        ulong feeBuffer = 11 * adaToLovelace;
        var response = coinSelection.GetCoinSelection(outputs, utxos, address, feeBuffer: 11 * adaToLovelace);

        //assert
        Assert.Equal(5, response.SelectedUtxos.Count);
        Assert.Equal(1, response.ChangeOutputs.Count);
        Assert.Equal(5, response.ChangeOutputs.First().Value.MultiAsset.Count);

        long totalSelected = 0;
        response.SelectedUtxos.ForEach(s => totalSelected = totalSelected + (long)s.Balance.Lovelaces);
        long totalOutput = 0;
        outputs.ForEach(o => totalOutput = totalOutput + (long)o.Value.Coin);
        long totalChange = 0;
        response.ChangeOutputs.ForEach(s => totalChange = totalChange + (long)s.Value.Coin);
        long finalChangeOutputChange = (long)response.ChangeOutputs.Last().Value.Coin;
        Assert.Equal(totalSelected, totalOutput + totalChange);
        Assert.True((ulong)finalChangeOutputChange >= feeBuffer);
    }

    [Fact]
    public void RandomImprove_BasicChange_Fee_Fail_Test()
    {
        //arrange
        var coinSelection = new CoinSelectionService(new RandomImproveStrategy(), new BasicChangeSelectionStrategy());
        var outputs = new List<TransactionOutput>() { output_100_ada_no_assets };
        var utxos = new List<Utxo>() { utxo_50_ada_no_assets, utxo_50_ada_no_assets, utxo_10_ada_no_assets, };

        //assert
        try
        {
            //act
            var response = coinSelection.GetCoinSelection(outputs, utxos, address, feeBuffer: 11 * adaToLovelace);
        }
        catch (Exception e)
        {
            //assert
            Assert.Equal("UTxOs have insufficient balance", e.Message);
        }
    }
}
