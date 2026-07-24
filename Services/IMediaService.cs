using System.Threading.Tasks;

namespace agaverosActividades.Services
{
    public interface IMediaService
    {
        Task<string?> TomarFotoAsync();
        Task<string?> ElegirDeGaleriaAsync();
    }
}