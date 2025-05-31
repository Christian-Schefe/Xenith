using ReactiveData.App;
using ReactiveData.Core;

namespace PianoRoll
{
    public class TrackObserver : IReactor<ReactiveTrack>
    {
        private readonly System.Action onVisibleChanged;

        private ReactiveTrack track;

        public TrackObserver(System.Action onVisibleChanged)
        {
            this.onVisibleChanged = onVisibleChanged;
        }

        private void OnChanged(bool _)
        {
            onVisibleChanged?.Invoke();
        }

        public void Bind(ReactiveTrack data)
        {
            track = data;
            data.isBGVisible.OnChanged += OnChanged;
        }

        public void Unbind()
        {
            track.isBGVisible.OnChanged -= OnChanged;
        }
    }
}