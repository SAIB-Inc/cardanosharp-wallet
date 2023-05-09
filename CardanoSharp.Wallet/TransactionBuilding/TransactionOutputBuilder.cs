using System;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;

namespace CardanoSharp.Wallet.TransactionBuilding
{
    public interface ITransactionOutputBuilder : IABuilder<TransactionOutput>
    {
        ITransactionOutputBuilder SetAddress(byte[] address);
        ITransactionOutputBuilder SetTransactionOutputValue(TransactionOutputValue value);
        ITransactionOutputBuilder SetDatumOption(DatumOption datumOption);
        ITransactionOutputBuilder SetScriptReference(ScriptReference scriptReference);
        ITransactionOutputBuilder SetOutputPurpose(OutputPurpose outputPurpose);
        ITransactionOutputBuilder SetMinUtxoOutput(
            byte[] address,
            ulong coin = 0,
            ITokenBundleBuilder? tokenBundleBuilder = null,
            DatumOption? datumOption = null,
            ScriptReference? scriptReference = null,
            OutputPurpose outputPurpose = OutputPurpose.Spend
        );
    }

    public class TransactionOutputBuilder : ABuilder<TransactionOutput>, ITransactionOutputBuilder
    {
        public TransactionOutputBuilder()
        {
            _model = new TransactionOutput();
        }

        private TransactionOutputBuilder(TransactionOutput model)
        {
            _model = model;
        }

        public static ITransactionOutputBuilder GetBuilder(TransactionOutput model)
        {
            if (model == null)
            {
                return new TransactionOutputBuilder();
            }
            return new TransactionOutputBuilder(model);
        }

        public static ITransactionOutputBuilder Create
        {
            get => new TransactionOutputBuilder();
        }

        public ITransactionOutputBuilder SetAddress(byte[] address)
        {
            _model.Address = address;
            return this;
        }

        public ITransactionOutputBuilder SetTransactionOutputValue(TransactionOutputValue value)
        {
            _model.Value = value;
            return this;
        }

        public ITransactionOutputBuilder SetDatumOption(DatumOption datumOption)
        {
            _model.DatumOption = datumOption;
            return this;
        }

        public ITransactionOutputBuilder SetScriptReference(ScriptReference scriptReference)
        {
            _model.ScriptReference = scriptReference;
            return this;
        }

        public ITransactionOutputBuilder SetOutputPurpose(OutputPurpose outputPurpose)
        {
            _model.OutputPurpose = outputPurpose;
            return this;
        }

        public ITransactionOutputBuilder SetMinUtxoOutput(
            byte[] address,
            ulong coin = 0,
            ITokenBundleBuilder? tokenBundleBuilder = null,
            DatumOption? datumOption = null,
            ScriptReference? scriptReference = null,
            OutputPurpose outputPurpose = OutputPurpose.Spend
        )
        {
            // First we create a transaction output builder with a dummy coin value
            ulong dummyCoin = (ulong)(CardanoUtility.adaOnlyMinUtxo); // We need a Dummy Coin for proper minUTXO calculation
            TransactionOutputBuilder transactionOutputBuilder = (TransactionOutputBuilder)
                TransactionOutputBuilder.Create.SetAddress(address).SetOutputPurpose(outputPurpose);

            if (tokenBundleBuilder is not null)
                transactionOutputBuilder.SetTransactionOutputValue(
                    new TransactionOutputValue { Coin = dummyCoin, MultiAsset = tokenBundleBuilder.Build() }
                );
            else
                transactionOutputBuilder.SetTransactionOutputValue(new TransactionOutputValue { Coin = dummyCoin });

            if (datumOption is not null)
                transactionOutputBuilder.SetDatumOption(datumOption);
            if (scriptReference is not null)
                transactionOutputBuilder.SetScriptReference(scriptReference);

            // Now we calculate the correct minUtxo coin value
            var transactionOutput = transactionOutputBuilder.Build();
            ulong finalCoin = Math.Max(transactionOutput.CalculateMinUtxoLovelace(), coin);
            if (tokenBundleBuilder is not null)
            {
                transactionOutputBuilder.SetTransactionOutputValue(
                    new TransactionOutputValue { Coin = finalCoin, MultiAsset = tokenBundleBuilder.Build() }
                );
            }
            else
                transactionOutputBuilder.SetTransactionOutputValue(new TransactionOutputValue { Coin = finalCoin });

            var minUtxoTransactionOutput = transactionOutputBuilder.Build();
            this.SetAddress(minUtxoTransactionOutput.Address)
                .SetTransactionOutputValue(minUtxoTransactionOutput.Value)
                .SetOutputPurpose(minUtxoTransactionOutput.OutputPurpose);

            if (minUtxoTransactionOutput.DatumOption is not null)
                this.SetDatumOption(minUtxoTransactionOutput.DatumOption);

            if (minUtxoTransactionOutput.ScriptReference is not null)
                this.SetScriptReference(minUtxoTransactionOutput.ScriptReference);

            return this;
        }
    }
}
