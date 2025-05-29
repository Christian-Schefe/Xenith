namespace ReactiveData.Core
{
    public interface IReactor<T>
    {
        public void Bind(T data);
        public void Unbind();
    }
}