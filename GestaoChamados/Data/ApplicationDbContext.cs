using Microsoft.EntityFrameworkCore;
using GestaoChamados.Models;

namespace GestaoChamados.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UsuarioModel> Usuarios { get; set; }
        public DbSet<ChamadoModel> Chamados { get; set; }
        public DbSet<ChatMessageModel> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da tabela Usuarios
            modelBuilder.Entity<UsuarioModel>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Senha).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            });

            // Configuração da tabela Chamados
            modelBuilder.Entity<ChamadoModel>(entity =>
            {
                entity.ToTable("Chamados");
                entity.HasKey(e => e.Protocolo);
                entity.Property(e => e.Protocolo).ValueGeneratedOnAdd();
                entity.Property(e => e.Assunto).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Descricao).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DataAbertura).IsRequired();
                entity.Property(e => e.UsuarioCriadorEmail).IsRequired().HasMaxLength(200);
                entity.Property(e => e.TecnicoAtribuidoEmail).HasMaxLength(200);
                entity.Property(e => e.AnexoNomeArquivo).HasMaxLength(500);
                entity.Property(e => e.Rating).HasDefaultValue(null);
            });

            // Configuração da tabela ChatMessages
            modelBuilder.Entity<ChatMessageModel>(entity =>
            {
                entity.ToTable("ChatMessages");
                entity.HasKey(e => new { e.TicketId, e.Timestamp });
                entity.Property(e => e.SenderEmail).IsRequired().HasMaxLength(200);
                entity.Property(e => e.SenderName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.MessageText).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.Timestamp).IsRequired();
            });
        }
    }
}
