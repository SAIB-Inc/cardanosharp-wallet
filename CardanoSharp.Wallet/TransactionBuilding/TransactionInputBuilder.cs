using CardanoSharp.Wallet.Models.Transactions;

namespace CardanoSharp.Wallet.TransactionBuilding
{
    public interface ITransactionInputBuilder : IABuilder<TransactionInput>
    {
        ITransactionInputBuilder SetTransactionId(byte[] transactionId);
        ITransactionInputBuilder SetTransactionIndex(uint transactionIndex);
        ITransactionInputBuilder SetOutput(TransactionOutput output);
    }

    public class TransactionInputBuilder : ABuilder<TransactionInput>, ITransactionInputBuilder
    {
        public TransactionInputBuilder()
        {
            _model = new TransactionInput();
        }

        private TransactionInputBuilder(TransactionInput model)
        {
            _model = model;
        }

        public static ITransactionInputBuilder GetBuilder(TransactionInput model)
        {
            if (model == null)
            {
                return new TransactionInputBuilder();
            }
            return new TransactionInputBuilder(model);
        }

        public static ITransactionInputBuilder Create
        {
            get => new TransactionInputBuilder();
        }

        public ITransactionInputBuilder SetTransactionId(byte[] transactionId)
        {
            _model.TransactionId = transactionId;
            return this;
        }

        public ITransactionInputBuilder SetTransactionIndex(uint transactionIndex)
        {
            _model.TransactionIndex = transactionIndex;
            return this;
        }

        public ITransactionInputBuilder SetOutput(TransactionOutput output)
        {
            _model.Output = output;
            return this;
        }
    }
}
