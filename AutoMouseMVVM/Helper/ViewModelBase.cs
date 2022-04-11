using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    sealed class CallerMemberNameAttribute : Attribute { }
}

namespace AutoMouseMVVM.Helper
{

    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyname = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }
        }
    }
}
