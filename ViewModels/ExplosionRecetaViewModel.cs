using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace agaverosActividades.ViewModels
{
    /// <summary>
    /// Muestra y permite editar la explosión de materia prima calculada para un insumo tipo receta.
    /// Recibe la lista por referencia desde RegistroActividadFormViewModel; al editar Cantidad aquí,
    /// el cambio se refleja directo en MateriaPrimaAgregada del form principal (mismos objetos).
    /// </summary>
    public partial class ExplosionRecetaViewModel : ObservableObject, IQueryAttributable
    {
        [ObservableProperty]
        private ObservableCollection<MateriaPrimaAgregadaItem> materiasPrimas = new();

        [ObservableProperty]
        private string descripcionInsumo = string.Empty;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("materiasPrimas", out var mpValue) && mpValue is List<MateriaPrimaAgregadaItem> lista)
                MateriasPrimas = new ObservableCollection<MateriaPrimaAgregadaItem>(lista);

            if (query.TryGetValue("descripcionInsumo", out var descValue) && descValue is string desc)
                DescripcionInsumo = desc;
        }

        [RelayCommand]
        private async Task Cerrar()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}