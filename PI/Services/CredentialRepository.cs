using SQLite;
using PI.Models;

namespace PI.Services
{
    public class CredentialRepository : ICredentialRepository
    {
        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private readonly string _databasePath;
        private readonly ICryptoService _cryptoService;

        public CredentialRepository(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "cofre.db3");
        }

        private async Task InitAsync()
        {
            if (_database is not null)
                return;

            await _initSemaphore.WaitAsync();
            try
            {
                if (_database is not null)
                    return;

                SQLitePCL.Batteries_V2.Init();
                _database = new SQLiteAsyncConnection(_databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
                await _database.CreateTableAsync<Credential>();
                await _database.CreateTableAsync<MasterKeyConfig>();
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        public async Task<List<Credential>> GetAllAsync()
        {
            await InitAsync();

            var list = await _database!.Table<Credential>()
                                      .OrderBy(c => c.Title)
                                      .ToListAsync();

            // Decifra campos em memória antes de entregar para a UI
            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(item.Password))
                {
                    item.Password = _cryptoService.Decrypt(item.Password);
                }

                if (!string.IsNullOrEmpty(item.Notes))
                {
                    item.Notes = _cryptoService.Decrypt(item.Notes);
                }
            }

            return list;
        }

        public async Task<Credential?> GetByIdAsync(int id)
        {
            await InitAsync();

            var item = await _database!.Table<Credential>()
                                      .Where(c => c.Id == id)
                                      .FirstOrDefaultAsync();

            if (item is not null)
            {
                if (!string.IsNullOrEmpty(item.Password))
                {
                    item.Password = _cryptoService.Decrypt(item.Password);
                }

                if (!string.IsNullOrEmpty(item.Notes))
                {
                    item.Notes = _cryptoService.Decrypt(item.Notes);
                }
            }

            return item;
        }

        public async Task<int> SaveAsync(Credential credential)
        {
            await InitAsync();

            // Cria entidade de banco com Password e Notes rigorosamente criptografados (AES-256-GCM)
            var dbEntity = new Credential
            {
                Id = credential.Id,
                Title = credential.Title,
                Username = credential.Username,
                Password = _cryptoService.Encrypt(credential.Password),
                Notes = _cryptoService.Encrypt(credential.Notes),
                CreatedAt = credential.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            int result;
            if (dbEntity.Id != 0)
            {
                result = await _database!.UpdateAsync(dbEntity);
            }
            else
            {
                dbEntity.CreatedAt = DateTime.UtcNow;
                result = await _database!.InsertAsync(dbEntity);
                credential.Id = dbEntity.Id;
            }

            credential.UpdatedAt = dbEntity.UpdatedAt;
            credential.CreatedAt = dbEntity.CreatedAt;

            return result;
        }

        public async Task<int> DeleteAsync(Credential credential)
        {
            await InitAsync();
            return await _database!.DeleteAsync(credential);
        }
    }
}
