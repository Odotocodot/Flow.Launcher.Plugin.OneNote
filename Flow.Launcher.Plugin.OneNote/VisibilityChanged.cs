namespace Flow.Launcher.Plugin.OneNote
{
    public delegate void VisibilityChangedEventHandler(bool isVisible);
    public class VisibilityChanged
    {
        private event VisibilityChangedEventHandler? OnVisibilityChanged;
        private readonly PluginInitContext context;
        public VisibilityChanged(PluginInitContext context)
        {
            this.context = context;
            context.API.VisibilityChanged += OnVisibilityChangedWrap;
        }

        private void OnVisibilityChangedWrap(object _, VisibilityChangedEventArgs e)
        {
            if (!context.CurrentPluginMetadata.Disabled)
            {
                OnVisibilityChanged?.Invoke(e.IsVisible);
            }
        }
        public void Subscribe(VisibilityChangedEventHandler action)
        {
            OnVisibilityChanged += action;
        }

        public void Dispose()
        {
            context.API.VisibilityChanged -= OnVisibilityChangedWrap;
            OnVisibilityChanged = null;
        }
    }
}