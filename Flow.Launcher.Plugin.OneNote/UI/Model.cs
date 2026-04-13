using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Flow.Launcher.Plugin.OneNote.UI
{
    public class Model : BaseModel
    {
        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return false;

            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
