using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace agaverosActividades.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
    private string titulo = string.Empty;
    private bool estaCargando;

    public string Titulo
    {
        get => titulo;
        set
        {
            if (titulo != value)
            {
                titulo = value;
                OnPropertyChanged();
            }
        }
    }

    public bool EstaCargando
    {
        get => estaCargando;
        set
        {
            if (estaCargando != value)
            {
                estaCargando = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string nombre = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
    }
}
