using agaverosActividades.Models;

namespace agaverosActividades.Services
{
    public interface IAuthService
    {
        Task<LogeoModel?> LoginAsync(string usuario, string password);
    }
}