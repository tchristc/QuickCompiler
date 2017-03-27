namespace QuickCompiler
{
    public interface IProvider<out T>
    {
        T Provide();
    }

    public abstract class Provider<T> : IProvider<T>
    {
        public T Provide()
        {
            return default(T);
        }
    }
}
