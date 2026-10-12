using System.Security.Cryptography;
using System.Text;
using SQLite;
using PI.Models;

namespace PI.Services
{
    public class CryptoService : ICryptoService
    {
        private const int Pbkdf2Iterations = 600_000;
        private const int SaltSizeBytes = 16;
        private const int KeySizeBytes = 32; // 256 bits
        private const int NonceSizeBytes = 12; // 96 bits (NIST SP 800-38D)
        private const int TagSizeBytes = 16; // 128 bits
        private const string VerificationPayload = "COFRE_DIGITAL_AUTH_VALIDATION_TOKEN";

        private readonly string _databasePath;
        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _dbSemaphore = new(1, 1);

        // Chave derivada mantida exclusivamente em memória
        private byte[]? _derivedKey;

        public bool IsUnlocked => _derivedKey is not null && _derivedKey.Length == KeySizeBytes;

        public CryptoService()
        {
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "cofre.db3");
        }

        private async Task InitDatabaseAsync()
        {
            if (_database is not null)
                return;

            await _dbSemaphore.WaitAsync();
            try
            {
                if (_database is not null)
                    return;

                SQLitePCL.Batteries_V2.Init();
                _database = new SQLiteAsyncConnection(_databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
                await _database.CreateTableAsync<MasterKeyConfig>();
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task<bool> HasMasterPasswordAsync()
        {
            await InitDatabaseAsync();
            var config = await _database!.Table<MasterKeyConfig>().FirstOrDefaultAsync();
            return config is not null && !string.IsNullOrEmpty(config.SaltBase64) && !string.IsNullOrEmpty(config.VerificationTokenBase64);
        }

        public async Task<bool> SetupMasterPasswordAsync(string masterPassword)
        {
            if (string.IsNullOrEmpty(masterPassword))
                return false;

            await InitDatabaseAsync();

            // 1. Gera salt aleatório de 16 bytes
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);

            // 2. Deriva a chave de 256 bits via PBKDF2-HMAC-SHA256 com 600.000 iterações em background thread
            byte[] key = await Task.Run(() => Rfc2898DeriveBytes.Pbkdf2(
                masterPassword,
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                KeySizeBytes));

            // 3. Cifra o token de verificação com AES-256-GCM usando a chave recém-derivada
            string encryptedToken = EncryptWithKey(VerificationPayload, key);

            // 4. Salva o salt e o token cifrado no banco SQLite
            var config = new MasterKeyConfig
            {
                Id = 1,
                SaltBase64 = Convert.ToBase64String(salt),
                VerificationTokenBase64 = encryptedToken,
                CreatedAt = DateTime.UtcNow
            };

            await _database!.InsertOrReplaceAsync(config);

            // 5. Armazena a chave derivada exclusivamente em memória
            Lock(); // Limpa estado anterior se houver
            _derivedKey = key;

            return true;
        }

        public async Task<bool> UnlockAsync(string masterPassword)
        {
            if (string.IsNullOrEmpty(masterPassword))
                return false;

            await InitDatabaseAsync();

            var config = await _database!.Table<MasterKeyConfig>().FirstOrDefaultAsync();
            if (config is null || string.IsNullOrEmpty(config.SaltBase64) || string.IsNullOrEmpty(config.VerificationTokenBase64))
                return false;

            byte[] salt = Convert.FromBase64String(config.SaltBase64);

            // Deriva chave candidata em background thread
            byte[] candidateKey = await Task.Run(() => Rfc2898DeriveBytes.Pbkdf2(
                masterPassword,
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                KeySizeBytes));

            try
            {
                // Tenta decifrar o token de validação com a chave candidata
                string decryptedPayload = DecryptWithKey(config.VerificationTokenBase64, candidateKey);

                if (decryptedPayload == VerificationPayload)
                {
                    Lock();
                    _derivedKey = candidateKey;
                    return true;
                }
            }
            catch (CryptographicException)
            {
                // Tag de autenticação inválida ou dados corrompidos: senha incorreta
            }
            catch
            {
                // Qualquer outra falha de integridade
            }

            // Descarta a chave candidata da memória caso incorreta
            Array.Clear(candidateKey, 0, candidateKey.Length);
            return false;
        }

        public void Lock()
        {
            if (_derivedKey is not null)
            {
                Array.Clear(_derivedKey, 0, _derivedKey.Length);
                _derivedKey = null;
            }
        }

        public string Encrypt(string plainText)
        {
            if (!IsUnlocked || _derivedKey is null)
                throw new InvalidOperationException("O cofre está bloqueado.");

            return EncryptWithKey(plainText, _derivedKey);
        }

        public string Decrypt(string cipherText)
        {
            if (!IsUnlocked || _derivedKey is null)
                throw new InvalidOperationException("O cofre está bloqueado.");

            return DecryptWithKey(cipherText, _derivedKey);
        }

        private static string EncryptWithKey(string? plainText, byte[] key)
        {
            plainText ??= string.Empty;
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plainText);

            // Nonce aleatório de 12 bytes gerado para cada gravação
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            byte[] tag = new byte[TagSizeBytes];
            byte[] ciphertext = new byte[plaintextBytes.Length];

            using (var aesGcm = new AesGcm(key, TagSizeBytes))
            {
                aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
            }

            // Pacote: nonce (12) + tag (16) + ciphertext (N)
            byte[] combined = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, NonceSizeBytes);
            Buffer.BlockCopy(tag, 0, combined, NonceSizeBytes, TagSizeBytes);
            Buffer.BlockCopy(ciphertext, 0, combined, NonceSizeBytes + TagSizeBytes, ciphertext.Length);

            return Convert.ToBase64String(combined);
        }

        private static string DecryptWithKey(string cipherText, byte[] key)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            byte[] combined = Convert.FromBase64String(cipherText);
            int minLength = NonceSizeBytes + TagSizeBytes;
            if (combined.Length < minLength)
                return string.Empty;

            byte[] nonce = new byte[NonceSizeBytes];
            byte[] tag = new byte[TagSizeBytes];
            int ciphertextLength = combined.Length - minLength;
            byte[] ciphertext = new byte[ciphertextLength];

            Buffer.BlockCopy(combined, 0, nonce, 0, NonceSizeBytes);
            Buffer.BlockCopy(combined, NonceSizeBytes, tag, 0, TagSizeBytes);
            Buffer.BlockCopy(combined, minLength, ciphertext, 0, ciphertextLength);

            byte[] plaintextBytes = new byte[ciphertextLength];

            using (var aesGcm = new AesGcm(key, TagSizeBytes))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);
            }

            return Encoding.UTF8.GetString(plaintextBytes);
        }
    }
}
