namespace PI.Services
{
    public interface ICryptoService
    {
        bool IsUnlocked { get; }
        Task<bool> HasMasterPasswordAsync();
        Task<bool> SetupMasterPasswordAsync(string masterPassword);
        Task<bool> UnlockAsync(string masterPassword);
        void Lock();
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
}
