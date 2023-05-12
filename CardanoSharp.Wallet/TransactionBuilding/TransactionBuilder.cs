using CardanoSharp.Wallet.Models.Transactions;

namespace CardanoSharp.Wallet.TransactionBuilding
{
    public interface ITransactionBuilder : IABuilder<Transaction>
    {
        ITransactionBuilder SetBody(ITransactionBodyBuilder bodyBuilder);
        ITransactionBuilder SetWitnesses(ITransactionWitnessSetBuilder witnessBuilder);
        ITransactionBuilder SetAuxData(IAuxiliaryDataBuilder auxDataBuilder);

        // Composable Builders
        ITransactionBodyBuilder bodyBuilder { get; set; }
        ITransactionWitnessSetBuilder witnessesBuilder { get; set; }
        IAuxiliaryDataBuilder auxDataBuilder { get; set; }
        ITransactionBuilder SetBodyBuilder(ITransactionBodyBuilder bodyBuilder);
        ITransactionBuilder SetWitnessesBuilder(ITransactionWitnessSetBuilder witnessesBuilder);
        ITransactionBuilder SetAuxDataBuilder(IAuxiliaryDataBuilder auxDataBuilder);
    }

    public partial class TransactionBuilder : ABuilder<Transaction>, ITransactionBuilder
    {
        public ITransactionBodyBuilder bodyBuilder { get; set; } = default!;
        public ITransactionWitnessSetBuilder witnessesBuilder { get; set; } = default!;
        public IAuxiliaryDataBuilder auxDataBuilder { get; set; } = default!;

        private TransactionBuilder()
        {
            _model = new Transaction();
        }

        private TransactionBuilder(Transaction model)
        {
            _model = model;
        }

        public static ITransactionBuilder GetBuilder(Transaction model)
        {
            if (model == null)
            {
                return new TransactionBuilder();
            }
            return new TransactionBuilder(model);
        }

        public static ITransactionBuilder Create
        {
            get => new TransactionBuilder();
        }

        public ITransactionBuilder SetBody(ITransactionBodyBuilder bodyBuilder)
        {
            _model.TransactionBody = bodyBuilder.Build();
            return this;
        }

        public ITransactionBuilder SetWitnesses(ITransactionWitnessSetBuilder witnessesBuilder)
        {
            _model.TransactionWitnessSet = witnessesBuilder.Build();
            return this;
        }

        public ITransactionBuilder SetAuxData(IAuxiliaryDataBuilder auxDataBuilder)
        {
            _model.AuxiliaryData = auxDataBuilder.Build();
            return this;
        }

        public ITransactionBuilder SetBodyBuilder(ITransactionBodyBuilder bodyBuilder)
        {
            this.bodyBuilder = bodyBuilder;
            return this;
        }

        public ITransactionBuilder SetWitnessesBuilder(ITransactionWitnessSetBuilder witnessesBuilder)
        {
            this.witnessesBuilder = witnessesBuilder;
            return this;
        }

        public ITransactionBuilder SetAuxDataBuilder(IAuxiliaryDataBuilder auxDataBuilder)
        {
            this.auxDataBuilder = auxDataBuilder;
            return this;
        }

        public override Transaction Build()
        {
            if (bodyBuilder != null)
                SetBody(bodyBuilder);

            if (witnessesBuilder != null)
                SetWitnesses(witnessesBuilder);

            if (auxDataBuilder != null)
                SetAuxData(auxDataBuilder);

            return _model;
        }
    }
}
