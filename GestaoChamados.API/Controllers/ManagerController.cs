using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestaoChamados.Data;
using GestaoChamados.Models;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Gerente")]
    public class ManagerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManagerController> _logger;

        public ManagerController(
            ApplicationDbContext context,
            ILogger<ManagerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retorna dados do dashboard gerencial
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<ManagerDashboardDto>> GetDashboard()
        {
            var todosChamados = await _context.Chamados.ToListAsync();

            // Status no banco: "Aberto" = Aguardando Atendente, "Em Atendimento", "Resolvido"
            var chamadosAbertos = todosChamados.Count(c => c.Status == "Aberto");
            var chamadosResolvidos = todosChamados.Count(c => c.Status == "Resolvido");
            var chamadosNaoAtendidos = todosChamados.Count(c => c.Status == "Aberto" || c.Status == "Aguardando Atendente");
            var chamadosEmAtendimento = todosChamados.Count(c => c.Status == "Em Atendimento");
            var totalChamados = todosChamados.Count;

            var taxaResolucao = totalChamados > 0
                ? Math.Round((double)chamadosResolvidos / totalChamados * 100, 2)
                : 0;

            var avaliacaoMedia = todosChamados
                .Where(c => c.Rating.HasValue)
                .Average(c => (double?)c.Rating) ?? 0;

            // Chamados por categoria
            var chamadosPorCategoria = todosChamados
                .GroupBy(c => ExtrairCategoria(c.Assunto))
                .Select(g => new ChamadoPorCategoriaDto
                {
                    Categoria = g.Key,
                    Quantidade = g.Count()
                })
                .OrderByDescending(x => x.Quantidade)
                .ToList();

            // Chamados por prioridade
            var chamadosPorPrioridade = todosChamados
                .GroupBy(c => ExtrairPrioridade(c.Descricao))
                .Select(g => new ChamadoPorPrioridadeDto
                {
                    Prioridade = g.Key,
                    Quantidade = g.Count()
                })
                .OrderByDescending(x => x.Quantidade)
                .ToList();

            // Desempenho por técnico
            var desempenhoPorTecnico = todosChamados
                .Where(c => !string.IsNullOrEmpty(c.TecnicoAtribuidoEmail) && c.Status == "Resolvido")
                .GroupBy(c => c.TecnicoAtribuidoEmail)
                .Select(g => new DesempenhoTecnicoDto
                {
                    Nome = g.Key!,
                    Resolvidos = g.Count()
                })
                .OrderByDescending(x => x.Resolvidos)
                .Take(10)
                .ToList();

            var totalUsuarios = await _context.Usuarios.CountAsync(u => u.Role == "Usuario");
            var totalTecnicos = await _context.Usuarios.CountAsync(u => u.Role == "Tecnico");

            var dashboard = new ManagerDashboardDto
            {
                ChamadosAbertos = chamadosAbertos,
                ChamadosResolvidos = chamadosResolvidos,
                ChamadosNaoAtendidos = chamadosNaoAtendidos,
                ChamadosEmAtendimento = chamadosEmAtendimento,
                TotalChamados = totalChamados,
                TotalUsuarios = totalUsuarios,
                TotalTecnicos = totalTecnicos,
                TaxaResolucao = taxaResolucao,
                AvaliaoMediaAtendimento = avaliacaoMedia,
                ChamadosPorCategoria = chamadosPorCategoria,
                ChamadosPorPrioridade = chamadosPorPrioridade,
                DesempenhoPorTecnico = desempenhoPorTecnico
            };

            return Ok(dashboard);
        }

        /// <summary>
        /// Lista todos os usuários
        /// </summary>
        [HttpGet("usuarios")]
        public async Task<ActionResult<IEnumerable<ListarUsuarioDto>>> GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new ListarUsuarioDto
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = u.Email,
                    Role = u.Role,
                    DataCriacao = DateTime.Now // UsuarioModel não tem DataCriacao
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        /// <summary>
        /// Cria novo usuário
        /// </summary>
        [HttpPost("usuarios")]
        public async Task<ActionResult<UsuarioModel>> CreateUsuario([FromBody] CriarEditarUsuarioDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Senha) || string.IsNullOrWhiteSpace(dto.Role))
            {
                return BadRequest("Todos os campos são obrigatórios.");
            }

            if (!new[] { "Usuario", "Tecnico", "Gerente" }.Contains(dto.Role))
            {
                return BadRequest("Papel inválido.");
            }

            if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest("Usuário com este email já existe.");
            }

            var senhaCriptografada = BCrypt.Net.BCrypt.HashPassword(dto.Senha);

            var novoUsuario = new UsuarioModel
            {
                Nome = dto.Nome,
                Email = dto.Email,
                Senha = senhaCriptografada,
                Role = dto.Role
            };

            _context.Usuarios.Add(novoUsuario);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Novo usuário criado: {Email} ({Role})", dto.Email, dto.Role);

            return CreatedAtAction(nameof(GetUsuarios), new { id = novoUsuario.Id }, novoUsuario);
        }

        /// <summary>
        /// Atualiza usuário existente
        /// </summary>
        [HttpPut("usuarios/{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, [FromBody] CriarEditarUsuarioDto dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Role))
            {
                return BadRequest("Campos obrigatórios não preenchidos.");
            }

            if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email && u.Id != id))
            {
                return BadRequest("Email já está em uso.");
            }

            usuario.Nome = dto.Nome;
            usuario.Email = dto.Email;
            usuario.Role = dto.Role;

            if (!string.IsNullOrWhiteSpace(dto.Senha))
            {
                usuario.Senha = BCrypt.Net.BCrypt.HashPassword(dto.Senha);
            }

            _context.Entry(usuario).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário atualizado: {Email}", dto.Email);

            return NoContent();
        }

        /// <summary>
        /// Deleta usuário
        /// </summary>
        [HttpDelete("usuarios/{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário deletado: {Email}", usuario.Email);

            return NoContent();
        }

        /// <summary>
        /// Retorna relatório detalhado com filtro de período
        /// </summary>
        [HttpGet("relatorio")]
        public async Task<ActionResult<RelatorioDetalhadoDto>> GetRelatorioDetalhado(
            [FromQuery] DateTime? dataInicio = null,
            [FromQuery] DateTime? dataFim = null)
        {
            _logger.LogInformation($"[ManagerController] GetRelatorioDetalhado - dataInicio: {dataInicio}, dataFim: {dataFim}");
            
            // Se não fornecido, usar últimos 30 dias
            var inicio = dataInicio ?? DateTime.Today.AddDays(-30);
            var fim = (dataFim ?? DateTime.Today).AddDays(1).Date; // Sempre incluir o dia completo

            _logger.LogInformation($"[ManagerController] Período ajustado - inicio: {inicio:yyyy-MM-dd HH:mm:ss}, fim: {fim:yyyy-MM-dd HH:mm:ss}");

            var todosChamados = await _context.Chamados
                .Where(c => c.DataAbertura >= inicio && c.DataAbertura < fim)
                .ToListAsync();

            _logger.LogInformation($"[ManagerController] Chamados encontrados: {todosChamados.Count}");

            var relatorio = new RelatorioDetalhadoDto
            {
                TotalChamados = todosChamados.Count,
                Abertos = todosChamados.Count(c => c.Status == "Aberto"),
                Resolvidos = todosChamados.Count(c => c.Status == "Resolvido"),
                EmAtendimento = todosChamados.Count(c => c.Status == "Em Atendimento"),
                NaoAtendidos = todosChamados.Count(c => c.Status == "Aberto" || c.Status == "Aguardando Atendente"),
                ChamadosPorTecnico = todosChamados
                    .Where(c => !string.IsNullOrEmpty(c.TecnicoAtribuidoEmail))
                    .GroupBy(c => c.TecnicoAtribuidoEmail)
                    .Select(g => new ChamadosPorTecnicoDto
                    {
                        Tecnico = g.Key!,
                        Total = g.Count(),
                        Resolvidos = g.Count(c => c.Status == "Resolvido"),
                        NotaMedia = g.Where(c => c.Rating.HasValue && c.Rating.Value > 0)
                                     .Select(c => (double)c.Rating!.Value)
                                     .DefaultIfEmpty(0)
                                     .Average()
                    })
                    .ToList()
            };

            _logger.LogInformation($"[ManagerController] Relatório gerado - Total: {relatorio.TotalChamados}, Resolvidos: {relatorio.Resolvidos}, Técnicos: {relatorio.ChamadosPorTecnico.Count}");

            return Ok(relatorio);
        }

        // Métodos auxiliares
        private string ExtrairCategoria(string assunto)
        {
            if (string.IsNullOrEmpty(assunto)) return "Outros";

            var assuntoLower = assunto.ToLower();
            if (assuntoLower.Contains("mouse") || assuntoLower.Contains("teclado") ||
                assuntoLower.Contains("monitor") || assuntoLower.Contains("impressora"))
                return "Hardware";

            if (assuntoLower.Contains("internet") || assuntoLower.Contains("rede") ||
                assuntoLower.Contains("wifi"))
                return "Conectividade";

            if (assuntoLower.Contains("senha") || assuntoLower.Contains("login") ||
                assuntoLower.Contains("acesso"))
                return "Acesso e Segurança";

            return "Outros";
        }

        private string ExtrairPrioridade(string descricao)
        {
            if (string.IsNullOrEmpty(descricao)) return "Normal";

            var descricaoLower = descricao.ToLower();
            if (descricaoLower.Contains("urgente") || descricaoLower.Contains("crítico"))
                return "Crítica";

            if (descricaoLower.Contains("alta") || descricaoLower.Contains("importante"))
                return "Alta";

            if (descricaoLower.Contains("baixa"))
                return "Baixa";

            return "Normal";
        }
    }

    // DTOs adicionais
    public class RelatorioDetalhadoDto
    {
        public int TotalChamados { get; set; }
        public int Abertos { get; set; }
        public int Resolvidos { get; set; }
        public int EmAtendimento { get; set; }
        public int NaoAtendidos { get; set; }
        public List<ChamadosPorTecnicoDto> ChamadosPorTecnico { get; set; } = new();
    }

    public class ChamadosPorTecnicoDto
    {
        public string Tecnico { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Resolvidos { get; set; }
        public double NotaMedia { get; set; }
    }
}
