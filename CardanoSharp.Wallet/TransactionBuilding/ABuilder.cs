namespace CardanoSharp.Wallet.TransactionBuilding
{
    public interface IABuilder<T>
    {
        T Build();
    }

    public abstract class ABuilder<T> : IABuilder<T>
    {
        protected T _model = default!;

        public virtual T Build()
        {
            return _model;
        }
    }
}
