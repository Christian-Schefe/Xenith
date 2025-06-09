namespace ReactiveData.Core
{
    public class Effect
    {
        private System.Action effect;
        private IReactive[] dependencies;

        public Effect(System.Action effect, params IReactive[] dependencies) : this(effect, true, dependencies) { }

        public Effect(System.Action effect, bool callImmediate = true, params IReactive[] dependencies)
        {
            this.effect = effect;
            this.dependencies = dependencies;

            foreach (var dependency in dependencies)
            {
                dependency.OnChanged += ExecuteEffect;
            }
            if (callImmediate)
            {
                ExecuteEffect();
            }
        }

        private void ExecuteEffect()
        {
            effect?.Invoke();
        }

        public void Dispose()
        {
            foreach (var dependency in dependencies)
            {
                dependency.OnChanged -= ExecuteEffect;
            }
            effect = null;
            dependencies = null;
        }
    }
}