using PI.Models;

namespace PI.Services
{
    public interface ICredentialRepository
    {
        Task<List<Credential>> GetAllAsync();
        Task<Credential?> GetByIdAsync(int id);
        Task<int> SaveAsync(Credential credential);
        Task<int> DeleteAsync(Credential credential);
    }
}
