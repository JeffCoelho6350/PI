using SQLite;

namespace PI.Models
{
    [Table("MasterKeyConfig")]
    public class MasterKeyConfig
    {
        [PrimaryKey]
        public int Id { get; set; } = 1;

        [NotNull]
        public string SaltBase64 { get; set; } = string.Empty;

        [NotNull]
        public string VerificationTokenBase64 { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
