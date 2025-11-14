using GestaoChamados.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using GestaoChamados.Services;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.Controllers
{
    [Authorize(Roles = "Tecnico, Usuario")]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly ApiService _apiService;

        public DashboardController(ILogger<DashboardController> logger, ApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }

        public async Task<IActionResult> FilaDeAtendimento()
        {
            try
            {
                _logger.LogInformation("[FilaDeAtendimento] Buscando fila via API...");
                
                // Busca fila via API
                var chamadosDto = await _apiService.GetAsync<List<ChamadoDto>>("/api/dashboard/fila");
                
                _logger.LogInformation($"[FilaDeAtendimento] Recebidos {chamadosDto?.Count ?? 0} chamados da API");
                
                if (chamadosDto == null || !chamadosDto.Any())
                {
                    _logger.LogWarning("[FilaDeAtendimento] Nenhum chamado na fila");
                    return View(new List<ChamadoModel>());
                }
                
                // Log do primeiro chamado para debug
                var primeiro = chamadosDto.First();
                _logger.LogInformation($"[FilaDeAtendimento] Primeiro chamado - Id: {primeiro.Id}, Titulo: {primeiro.Titulo}, Email: {primeiro.UsuarioEmail}");
                
                // Converte DTOs para Models
                var chamados = chamadosDto.Select(dto => new ChamadoModel
                {
                    Protocolo = dto.Id,
                    Assunto = dto.Titulo,
                    Descricao = dto.Descricao,
                    Status = dto.Status,
                    DataAbertura = dto.DataCriacao,
                    UsuarioCriadorEmail = dto.UsuarioEmail
                    // Não mapeia TecnicoAtribuidoEmail - chamados na fila não têm técnico ainda
                }).OrderBy(c => c.DataAbertura).ToList();
                
                _logger.LogInformation($"[FilaDeAtendimento] Retornando {chamados.Count} chamados para a view");
                
                return View(chamados);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"[FilaDeAtendimento] Erro ao buscar fila: {ex.Message}");
                _logger.LogError($"[FilaDeAtendimento] StackTrace: {ex.StackTrace}");
                return View(new List<ChamadoModel>());
            }
        }

        public async Task<IActionResult> Index()
        {
            // LOG: Informações do usuário autenticado
            _logger.LogInformation("=== DASHBOARD INDEX ACESSADO ===");
            _logger.LogInformation("User.Identity.Name: {Name}", User.Identity?.Name);
            _logger.LogInformation("User.Identity.IsAuthenticated: {IsAuth}", User.Identity?.IsAuthenticated);
            
            // LOG: Todos os claims do usuário
            var claims = User.Claims.ToList();
            _logger.LogInformation("Total de Claims: {Count}", claims.Count);
            foreach (var claim in claims)
            {
                _logger.LogInformation("Claim -> Type: {Type}, Value: {Value}", claim.Type, claim.Value);
            }
            
            // LOG: Verifica roles específicas
            _logger.LogInformation("User.IsInRole('Tecnico'): {IsTecnico}", User.IsInRole("Tecnico"));
            _logger.LogInformation("User.IsInRole('Usuario'): {IsUsuario}", User.IsInRole("Usuario"));
            _logger.LogInformation("================================");

            try
            {
                // Busca dados do dashboard via API
                var dashboardDto = await _apiService.GetAsync<DashboardDataDto>("/api/dashboard");
                
                if (dashboardDto == null)
                {
                    _logger.LogWarning("DashboardDataDto retornou null da API");
                    return View(new DashboardViewModel());
                }
                
                // Converte DTO para ViewModel
                var viewModel = new DashboardViewModel
                {
                    TotalChamados = dashboardDto.TotalChamados,
                    ChamadosAbertos = dashboardDto.ChamadosAbertos,
                    ChamadosEmAtendimento = dashboardDto.ChamadosEmAtendimento,
                    ChamadosResolvidos = dashboardDto.ChamadosResolvidos,
                    ChamadosNaFila = dashboardDto.ChamadosNaFila,
                    PercentualResolvidos = dashboardDto.PercentualResolvidos,
                    NotaMediaSatisfacao = dashboardDto.NotaMediaSatisfacao,
                    TotalAvaliacoes = dashboardDto.TotalAvaliacoes
                };
                
                // Mapeia status labels e counts (sistema geral)
                if (dashboardDto.StatusGroups != null)
                {
                    viewModel.StatusLabels = dashboardDto.StatusGroups.Select(g => g.Status).ToList();
                    viewModel.StatusCounts = dashboardDto.StatusGroups.Select(g => g.Count).ToList();
                }
                
                // **NOVO: Status do técnico específico**
                viewModel.MeuStatusLabels = dashboardDto.MeuStatusLabels ?? new List<string>();
                viewModel.MeuStatusCounts = dashboardDto.MeuStatusCounts ?? new List<int>();
                
                // Mapeia rankings
                if (dashboardDto.TopUsuarios != null)
                {
                    viewModel.TopUsuarios = dashboardDto.TopUsuarios.Select(u => new Models.RankingModel
                    {
                        Nome = u.Nome,
                        Contagem = u.Contagem
                    }).ToList();
                }
                
                if (dashboardDto.TopTecnicos != null)
                {
                    viewModel.TopTecnicos = dashboardDto.TopTecnicos.Select(t => new Models.RankingModel
                    {
                        Nome = t.Nome,
                        Contagem = t.Contagem
                    }).ToList();
                }
                
                return View(viewModel);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Erro ao buscar dados do dashboard: {ex.Message}");
                return View(new DashboardViewModel());
            }
        }
    }
}