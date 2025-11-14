using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoChamados.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGerenteSenha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Atualizar senha do gerente de hash SHA256 para texto plano
            migrationBuilder.Sql(@"
                UPDATE [Usuarios]
                SET [Senha] = 'senha123'
                WHERE [Email] = 'gerente@sistema.com'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
