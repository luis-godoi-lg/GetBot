using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GestaoChamados.Data;
using GestaoChamados.Models;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retorna dados do dashboard baseado no papel do usuário
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<DashboardDataDto>> GetDashboardData()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            _logger.LogInformation("Dashboard acessado por: {Email} (Role: {Role})", userEmail, userRole);

            var dashboardData = new DashboardDataDto();

            if (userRole == "Tecnico" || userRole == "Gerente")
            {
                // Dados gerais para técnicos e gerentes
                var todosChamados = await _context.Chamados.ToListAsync();

                dashboardData.TotalChamados = todosChamados.Count;
                dashboardData.ChamadosAbertos = todosChamados.Count(c => c.Status == "Aberto");
                dashboardData.ChamadosEmAtendimento = todosChamados.Count(c => c.Status == "Em Atendimento");
                dashboardData.ChamadosResolvidos = todosChamados.Count(c => c.Status == "Resolvido");
                dashboardData.ChamadosNaFila = todosChamados.Count(c => c.Status == "Aguardando Atendente");

                // **NOVO: Dados específicos do técnico logado**
                var meusChamados = todosChamados.Where(c => c.TecnicoAtribuidoEmail == userEmail).ToList();
                var meusChamadosResolvidos = meusChamados.Count(c => c.Status == "Resolvido");
                var meusChamadosTotal = meusChamados.Count;
                
                // Percentual de resolução
                dashboardData.PercentualResolvidos = meusChamadosTotal > 0 
                    ? Math.Round((double)meusChamadosResolvidos / meusChamadosTotal * 100, 1) 
                    : 0;

                // Nota média de satisfação (apenas chamados com rating)
                var chamadosComRating = meusChamados.Where(c => c.Rating.HasValue && c.Rating.Value > 0).ToList();
                dashboardData.NotaMediaSatisfacao = chamadosComRating.Any() 
                    ? Math.Round(chamadosComRating.Average(c => c.Rating!.Value), 2) 
                    : 0;
                
                dashboardData.TotalAvaliacoes = chamadosComRating.Count;

                // Agrupar por status (TODOS os chamados do sistema para o gráfico geral)
                var statusGroups = todosChamados.GroupBy(c => c.Status)
                    .Select(g => new StatusGroup { Status = g.Key, Count = g.Count() })
                    .ToList();
                dashboardData.StatusLabels = statusGroups.Select(g => g.Status).ToList();
                dashboardData.StatusCounts = statusGroups.Select(g => g.Count).ToList();

                // **NOVO: Status apenas dos MEUS chamados (para gráfico do técnico)**
                var meuStatusGroups = meusChamados.GroupBy(c => c.Status)
                    .Select(g => new StatusGroup { Status = g.Key, Count = g.Count() })
                    .ToList();
                dashboardData.MeuStatusLabels = meuStatusGroups.Select(g => g.Status).ToList();
                dashboardData.MeuStatusCounts = meuStatusGroups.Select(g => g.Count).ToList();

                // Top usuários
                dashboardData.TopUsuarios = todosChamados
                    .GroupBy(c => c.UsuarioCriadorEmail)
                    .Select(g => new Shared.DTOs.RankingModel { Nome = g.Key ?? "Desconhecido", Contagem = g.Count() })
                    .OrderByDescending(r => r.Contagem)
                    .Take(5)
                    .ToList();

                // Top técnicos
                dashboardData.TopTecnicos = todosChamados
                    .Where(c => c.Status == "Resolvido" && c.TecnicoAtribuidoEmail != null)
                    .GroupBy(c => c.TecnicoAtribuidoEmail!)
                    .Select(g => new Shared.DTOs.RankingModel { Nome = g.Key, Contagem = g.Count() })
                    .OrderByDescending(r => r.Contagem)
                    .Take(5)
                    .ToList();
            }
            else // Usuario comum
            {
                var meusChamados = await _context.Chamados
                    .Where(c => c.UsuarioCriadorEmail == userEmail)
                    .ToListAsync();

                dashboardData.TotalChamados = meusChamados.Count;
                dashboardData.ChamadosAbertos = meusChamados.Count(c => c.Status == "Aberto");
                dashboardData.ChamadosEmAtendimento = meusChamados.Count(c => c.Status == "Em Atendimento");
                dashboardData.ChamadosResolvidos = meusChamados.Count(c => c.Status == "Resolvido");
                dashboardData.ChamadosNaFila = await _context.Chamados.CountAsync(c => c.Status == "Aguardando Atendente");

                var statusGroups = meusChamados.GroupBy(c => c.Status)
                    .Select(g => new StatusGroup { Status = g.Key, Count = g.Count() })
                    .ToList();
                dashboardData.StatusLabels = statusGroups.Select(g => g.Status).ToList();
                dashboardData.StatusCounts = statusGroups.Select(g => g.Count).ToList();
            }

            return Ok(dashboardData);
        }

        /// <summary>
        /// Retorna chamados na fila de atendimento
        /// </summary>
        [HttpGet("fila")]
        [Authorize(Roles = "Tecnico,Gerente")]
        public async Task<ActionResult<IEnumerable<Shared.DTOs.ChamadoDto>>> GetFilaAtendimento()
        {
            _logger.LogInformation("[API GetFilaAtendimento] Buscando chamados aguardando atendimento...");
            
            var chamadosNaFila = await _context.Chamados
                .Where(c => c.Status == "Aguardando Atendente")
                .OrderBy(c => c.DataAbertura)
                .ToListAsync();

            _logger.LogInformation($"[API GetFilaAtendimento] Encontrados {chamadosNaFila.Count} chamados na fila");

            // Mapear ChamadoModel (entidade) para ChamadoDto (DTO)
            var chamadosDto = chamadosNaFila.Select(c => new Shared.DTOs.ChamadoDto
            {
                Id = c.Protocolo,                      // Mapeia Protocolo → Id
                Titulo = c.Assunto,                    // Mapeia Assunto → Titulo
                Descricao = c.Descricao,
                Status = c.Status,
                UsuarioEmail = c.UsuarioCriadorEmail,  // Mapeia UsuarioCriadorEmail → UsuarioEmail
                DataCriacao = c.DataAbertura,          // Mapeia DataAbertura → DataCriacao
                TecnicoNome = c.TecnicoAtribuidoEmail  // Nome/Email do técnico (pode ser null)
            }).ToList();

            if (chamadosDto.Any())
            {
                var primeiro = chamadosDto.First();
                _logger.LogInformation($"[API GetFilaAtendimento] Primeiro chamado - Id: {primeiro.Id}, Titulo: {primeiro.Titulo}, Email: {primeiro.UsuarioEmail}");
            }

            return Ok(chamadosDto);
        }
    }

    // DTOs
    public class DashboardDataDto
    {
        public int TotalChamados { get; set; }
        public int ChamadosAbertos { get; set; }
        public int ChamadosEmAtendimento { get; set; }
        public int ChamadosResolvidos { get; set; }
        public int ChamadosNaFila { get; set; }
        
        // Dados do sistema (gráfico geral)
        public List<string> StatusLabels { get; set; } = new();
        public List<int> StatusCounts { get; set; } = new();
        
        // **NOVO: Dados específicos do técnico**
        public List<string> MeuStatusLabels { get; set; } = new();
        public List<int> MeuStatusCounts { get; set; } = new();
        public double PercentualResolvidos { get; set; }
        public double NotaMediaSatisfacao { get; set; }
        public int TotalAvaliacoes { get; set; }
        
        public List<Shared.DTOs.RankingModel> TopUsuarios { get; set; } = new();
        public List<Shared.DTOs.RankingModel> TopTecnicos { get; set; } = new();
    }

    public class StatusGroup
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
