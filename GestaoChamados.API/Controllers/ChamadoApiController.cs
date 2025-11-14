using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GestaoChamados.Data;
using GestaoChamados.Shared.DTOs;
using GestaoChamados.Models;
using Microsoft.AspNetCore.SignalR;
using GestaoChamados.Hubs;

namespace GestaoChamados.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChamadosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChamadosController> _logger;
        private readonly IHubContext<SupportHub> _hubContext;

        public ChamadosController(
            ApplicationDbContext context,
            ILogger<ChamadosController> logger,
            IHubContext<SupportHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Lista todos os chamados (filtra por usuário se não for técnico/gerente)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChamadoDto>>> GetChamados(
            [FromQuery] string? status = null,
            [FromQuery] int? protocolo = null)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation($"[GetChamados] Usuário: {userEmail}, Role: {userRole}");

            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            var query = _context.Chamados.AsQueryable();

            // Filtra por usuário se não for técnico ou gerente
            if (userRole != "Tecnico" && userRole != "Gerente" && userRole != "Admin")
            {
                _logger.LogInformation($"[GetChamados] Filtrando chamados do usuário: {userEmail}");
                query = query.Where(c => c.UsuarioCriadorEmail == userEmail);
            }
            else
            {
                _logger.LogInformation($"[GetChamados] Usuário {userRole} - retornando todos os chamados");
            }

            // Filtros opcionais
            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => c.Status == status);

            if (protocolo.HasValue)
                query = query.Where(c => c.Protocolo == protocolo.Value);

            var chamados = await query
                .OrderByDescending(c => c.DataAbertura)
                .Select(c => new ChamadoDto
                {
                    Id = c.Protocolo, // Mapear Protocolo para Id
                    Titulo = c.Assunto, // Mapear Assunto para Titulo
                    Descricao = c.Descricao,
                    Status = c.Status,
                    Prioridade = "Media", // Valor padrão
                    DataCriacao = c.DataAbertura, // Mapear DataAbertura para DataCriacao
                    DataFinalizacao = null,
                    UsuarioId = 0, // Não temos UsuarioId no modelo antigo
                    UsuarioNome = c.UsuarioCriadorEmail, // Usar email como nome temporário
                    UsuarioEmail = c.UsuarioCriadorEmail,
                    TecnicoId = null,
                    TecnicoNome = c.TecnicoAtribuidoEmail,
                    Rating = c.Rating
                })
                .ToListAsync();

            _logger.LogInformation($"[GetChamados] Retornando {chamados.Count} chamados para {userEmail}");
            
            return Ok(chamados);
        }

        /// <summary>
        /// Busca chamado por protocolo
        /// </summary>
        [HttpGet("{protocolo}")]
        public async Task<ActionResult<ChamadoDto>> GetChamado(int protocolo)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var chamado = await _context.Chamados
                .FirstOrDefaultAsync(c => c.Protocolo == protocolo);

            if (chamado == null)
                return NotFound(new { message = "Chamado não encontrado" });

            // Verifica permissão
            if (userRole != "Tecnico" && userRole != "Gerente" && userRole != "Admin" && chamado.UsuarioCriadorEmail != userEmail)
                return Forbid();

            return Ok(new ChamadoDto
            {
                Id = chamado.Protocolo,
                Titulo = chamado.Assunto,
                Descricao = chamado.Descricao,
                Status = chamado.Status,
                Prioridade = "Media",
                DataCriacao = chamado.DataAbertura,
                DataFinalizacao = null,
                UsuarioId = 0,
                UsuarioNome = chamado.UsuarioCriadorEmail,
                UsuarioEmail = chamado.UsuarioCriadorEmail,
                TecnicoId = null,
                TecnicoNome = chamado.TecnicoAtribuidoEmail,
                Rating = chamado.Rating
            });
        }

        /// <summary>
        /// Cria novo chamado
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ChamadoDto>> CreateChamado([FromBody] CriarChamadoDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            
            _logger.LogInformation($"[CreateChamado] Criando chamado para usuário: {userEmail}");
            _logger.LogInformation($"[CreateChamado] Título: {request.Titulo}");
            _logger.LogInformation($"[CreateChamado] Claims disponíveis: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("[CreateChamado] Email do usuário não encontrado nas claims!");
                return Unauthorized();
            }

            var chamado = new ChamadoModel
            {
                Assunto = request.Titulo,
                Descricao = request.Descricao,
                Status = request.Status ?? "Aberto", // ✅ Usa status do request ou "Aberto" por padrão
                DataAbertura = DateTime.Now,
                UsuarioCriadorEmail = userEmail
            };

            _context.Chamados.Add(chamado);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"[CreateChamado] Chamado #{chamado.Protocolo} criado com status '{chamado.Status}' para {userEmail}");

            // ✅ Só notificar técnicos se status for "Aguardando Atendente"
            if (chamado.Status == "Aguardando Atendente")
            {
                try
                {
                    await _hubContext.Clients.Group("Tecnicos").SendAsync("NovoUsuarioNaFila", new
                    {
                        Id = chamado.Protocolo,
                        Titulo = chamado.Assunto,
                        Usuario = userEmail,
                        Status = chamado.Status
                    });
                    _logger.LogInformation($"[CreateChamado] SignalR notificado - NovoUsuarioNaFila para chamado #{chamado.Protocolo}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[CreateChamado] Erro ao enviar notificação SignalR: {ex.Message}");
                }
            }

            var dto = new ChamadoDto
            {
                Id = chamado.Protocolo,
                Titulo = chamado.Assunto,
                Descricao = chamado.Descricao,
                Status = chamado.Status,
                Prioridade = request.Prioridade,
                DataCriacao = chamado.DataAbertura,
                DataFinalizacao = null,
                UsuarioId = 0,
                UsuarioNome = chamado.UsuarioCriadorEmail,
                UsuarioEmail = chamado.UsuarioCriadorEmail,
                TecnicoId = null,
                TecnicoNome = chamado.TecnicoAtribuidoEmail,
                Rating = chamado.Rating
            };

            return CreatedAtAction(nameof(GetChamado), new { protocolo = chamado.Protocolo }, dto);
        }

        /// <summary>
        /// Finaliza chamado
        /// </summary>
        [HttpPost("{protocolo}/finalizar")]
        public async Task<IActionResult> FinalizarChamado(int protocolo)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var chamado = await _context.Chamados.FindAsync(protocolo);
            if (chamado == null)
                return NotFound(new { message = "Chamado não encontrado" });

            // Verifica permissão
            if (userRole != "Tecnico" && userRole != "Gerente" && userRole != "Admin" && chamado.UsuarioCriadorEmail != userEmail)
                return Forbid();

            chamado.Status = "Resolvido";

            _context.Entry(chamado).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // ✅ Notificar via SignalR que o status mudou (Desktop/Mobile)
            try
            {
                await _hubContext.Clients.Group($"ticket-{protocolo}")
                    .SendAsync("TicketStatusChanged", "Resolvido");
                
                _logger.LogInformation($"[FinalizarChamado] SignalR TicketStatusChanged enviado para ticket-{protocolo}");
                
                // ✅ Enviar evento específico de pesquisa para Web MVC
                await _hubContext.Clients.Group($"ticket-{protocolo}")
                    .SendAsync("ShowSatisfactionSurvey", protocolo.ToString(), chamado.UsuarioCriadorEmail);
                
                _logger.LogInformation($"[FinalizarChamado] SignalR ShowSatisfactionSurvey enviado para {chamado.UsuarioCriadorEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[FinalizarChamado] Erro ao enviar notificação SignalR: {ex.Message}");
            }

            return Ok(new { message = "Chamado finalizado com sucesso" });
        }

        /// <summary>
        /// Assume um chamado para atendimento (apenas técnico/gerente/admin)
        /// </summary>
        [HttpPost("{protocolo}/assumir")]
        [Authorize(Roles = "Tecnico,Gerente,Admin")]
        public async Task<IActionResult> AssumirChamado(int protocolo)
        {
            _logger.LogInformation($"[AssumirChamado] Recebida requisição para assumir chamado {protocolo}");
            
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            
            _logger.LogInformation($"[AssumirChamado] Técnico: {userName} ({userEmail})");

            var chamado = await _context.Chamados.FindAsync(protocolo);
            if (chamado == null)
            {
                _logger.LogWarning($"[AssumirChamado] Chamado {protocolo} não encontrado");
                return NotFound(new { message = "Chamado não encontrado" });
            }

            _logger.LogInformation($"[AssumirChamado] Chamado encontrado. Status atual: {chamado.Status}");

            if (chamado.Status != "Aberto" && chamado.Status != "Aguardando Atendente")
            {
                _logger.LogWarning($"[AssumirChamado] Chamado {protocolo} já está em atendimento ou finalizado. Status: {chamado.Status}");
                return BadRequest(new { message = "Este chamado já está sendo atendido ou foi finalizado" });
            }

            chamado.Status = "Em Atendimento";
            chamado.TecnicoAtribuidoEmail = userEmail;
            
            // Adicionar informação ao histórico
            if (!string.IsNullOrEmpty(chamado.Descricao))
            {
                chamado.Descricao += $"\n\n--- CHAMADO ASSUMIDO POR {userName} ({userEmail}) em {DateTime.Now:dd/MM/yyyy HH:mm} ---";
            }

            _context.Entry(chamado).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"[AssumirChamado] Chamado {protocolo} assumido com sucesso por {userEmail}");

            // ✅ NOTIFICAR VIA SIGNALR
            try
            {
                // 1. Notificar o cliente específico (grupo do ticket)
                await _hubContext.Clients.Group($"ticket-{protocolo}")
                    .SendAsync("ChamadoAssumido", protocolo);
                
                _logger.LogInformation($"[AssumirChamado] SignalR: Notificado grupo ticket-{protocolo}");
                
                // 2. Notificar todos os técnicos (atualizar dashboard)
                await _hubContext.Clients.Group("Tecnicos")
                    .SendAsync("ChamadoAssumido", protocolo);
                
                _logger.LogInformation($"[AssumirChamado] SignalR: Notificado grupo Tecnicos");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[AssumirChamado] Erro ao enviar notificação SignalR: {ex.Message}");
            }

            return Ok(new { 
                message = "Chamado assumido com sucesso",
                tecnicoNome = userName
            });
        }

        /// <summary>
        /// Solicita atendimento humano (muda status para "Aguardando Atendente")
        /// </summary>
        [HttpPost("{protocolo}/solicitar-atendimento")]
        public async Task<IActionResult> SolicitarAtendimento(int protocolo)
        {
            _logger.LogInformation($"[SolicitarAtendimento] Recebida requisição para chamado {protocolo}");
            
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            var chamado = await _context.Chamados.FindAsync(protocolo);
            if (chamado == null)
            {
                _logger.LogWarning($"[SolicitarAtendimento] Chamado {protocolo} não encontrado");
                return NotFound(new { message = "Chamado não encontrado" });
            }

            // Verificar se usuário é dono do chamado
            if (chamado.UsuarioCriadorEmail != userEmail)
            {
                _logger.LogWarning($"[SolicitarAtendimento] Usuário {userEmail} tentou solicitar atendimento para chamado de outro usuário");
                return Forbid();
            }

            _logger.LogInformation($"[SolicitarAtendimento] Chamado encontrado. Status atual: {chamado.Status}");

            // Atualizar status
            chamado.Status = "Aguardando Atendente";
            
            // Adicionar informação ao histórico
            if (!string.IsNullOrEmpty(chamado.Descricao))
            {
                chamado.Descricao += $"\n\n--- ATENDIMENTO HUMANO SOLICITADO em {DateTime.Now:dd/MM/yyyy HH:mm} ---";
            }

            _context.Entry(chamado).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"[SolicitarAtendimento] Status do chamado {protocolo} alterado para 'Aguardando Atendente'");

            // ✅ Notificar técnicos via SignalR
            try
            {
                await _hubContext.Clients.Group("Tecnicos").SendAsync("NovoUsuarioNaFila", new
                {
                    Id = chamado.Protocolo,
                    Titulo = chamado.Assunto,
                    Usuario = userEmail,
                    Status = chamado.Status
                });
                _logger.LogInformation($"[SolicitarAtendimento] SignalR notificado - NovoUsuarioNaFila para chamado #{chamado.Protocolo}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[SolicitarAtendimento] Erro ao enviar notificação SignalR: {ex.Message}");
            }

            return Ok(new { 
                message = "Atendimento humano solicitado com sucesso",
                protocolo = chamado.Protocolo
            });
        }

        /// <summary>
        /// Exclui chamado (apenas técnico/gerente/admin)
        /// </summary>
        [HttpDelete("{protocolo}")]
        [Authorize(Roles = "Tecnico,Gerente,Admin")]
        public async Task<IActionResult> DeleteChamado(int protocolo)
        {
            var chamado = await _context.Chamados.FindAsync(protocolo);
            if (chamado == null)
                return NotFound(new { message = "Chamado não encontrado" });

            _context.Chamados.Remove(chamado);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Lista chamados na fila de atendimento (apenas técnico/gerente/admin)
        /// </summary>
        [HttpGet("fila")]
        [Authorize(Roles = "Tecnico,Gerente,Admin")]
        public async Task<ActionResult<IEnumerable<ChamadoDto>>> GetFilaAtendimento()
        {
            _logger.LogInformation("[GetFilaAtendimento] Buscando chamados aguardando atendimento...");
            
            var chamados = await _context.Chamados
                .Where(c => c.Status == "Aguardando Atendente")
                .OrderBy(c => c.DataAbertura)
                .Select(c => new ChamadoDto
                {
                    Id = c.Protocolo,
                    Titulo = c.Assunto,
                    Descricao = c.Descricao,
                    Status = c.Status,
                    Prioridade = "Media",
                    DataCriacao = c.DataAbertura,
                    DataFinalizacao = null,
                    UsuarioId = 0,
                    UsuarioNome = c.UsuarioCriadorEmail,
                    UsuarioEmail = c.UsuarioCriadorEmail,
                    TecnicoId = null,
                    TecnicoNome = c.TecnicoAtribuidoEmail,
                    Rating = c.Rating
                })
                .ToListAsync();

            return Ok(chamados);
        }

        /// <summary>
        /// Marcar chamado como resolvido (apenas técnico/gerente/admin)
        /// </summary>
        [HttpPost("{protocolo}/resolver")]
        [Authorize(Roles = "Tecnico,Gerente,Admin")]
        public async Task<IActionResult> MarcarComoResolvido(int protocolo)
        {
            var chamado = await _context.Chamados.FindAsync(protocolo);
            if (chamado == null)
                return NotFound(new { message = "Chamado não encontrado" });

            if (chamado.Status == "Resolvido")
                return BadRequest(new { message = "Chamado já está resolvido" });

            chamado.Status = "Resolvido";
            _context.Entry(chamado).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"[MarcarComoResolvido] Chamado #{protocolo} marcado como resolvido");

            // ✅ Notificar via SignalR que o status mudou (Desktop/Mobile)
            try
            {
                await _hubContext.Clients.Group($"ticket-{protocolo}")
                    .SendAsync("TicketStatusChanged", "Resolvido");
                
                _logger.LogInformation($"[MarcarComoResolvido] SignalR TicketStatusChanged enviado para ticket-{protocolo}");
                
                // ✅ Enviar evento específico de pesquisa para Web MVC
                await _hubContext.Clients.Group($"ticket-{protocolo}")
                    .SendAsync("ShowSatisfactionSurvey", protocolo.ToString(), chamado.UsuarioCriadorEmail);
                
                _logger.LogInformation($"[MarcarComoResolvido] SignalR ShowSatisfactionSurvey enviado para {chamado.UsuarioCriadorEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[MarcarComoResolvido] Erro ao enviar notificação SignalR: {ex.Message}");
            }

            return Ok(new { message = "Chamado marcado como resolvido com sucesso" });
        }

        /// <summary>
        /// Avaliar chamado resolvido
        /// </summary>
        [HttpPost("{protocolo}/avaliar")]
        public async Task<IActionResult> AvaliarChamado(int protocolo, [FromBody] AvaliacaoDto avaliacaoDto)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            
            var chamado = await _context.Chamados.FindAsync(protocolo);
            if (chamado == null)
                return NotFound(new { message = "Chamado não encontrado" });

            // Verifica se é o dono do chamado
            if (chamado.UsuarioCriadorEmail != userEmail)
                return Forbid();

            if (chamado.Status != "Resolvido")
                return BadRequest(new { message = "Só é possível avaliar chamados resolvidos" });

            if (avaliacaoDto.Rating < 1 || avaliacaoDto.Rating > 5)
                return BadRequest(new { message = "A avaliação deve estar entre 1 e 5" });

            chamado.Rating = avaliacaoDto.Rating;
            _context.Entry(chamado).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Chamado #{protocolo} avaliado com {avaliacaoDto.Rating} estrelas");

            return Ok(new { message = $"Avaliação de {avaliacaoDto.Rating} estrela(s) registrada com sucesso" });
        }

        /// <summary>
        /// Fechar/finalizar chamado (marcar como resolvido pelo usuário)
        /// </summary>
        [HttpPost("{protocolo}/fechar")]
        public async Task<IActionResult> FecharChamado(int protocolo)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            
            var chamado = await _context.Chamados.FindAsync(protocolo);
            if (chamado == null)
                return NotFound(new { message = "Chamado não encontrado" });

            // Verifica se é o dono do chamado
            if (chamado.UsuarioCriadorEmail != userEmail)
                return Forbid();

            if (chamado.Status == "Resolvido")
                return BadRequest(new { message = "Chamado já está fechado" });

            chamado.Status = "Resolvido";
            _context.Entry(chamado).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"[FecharChamado] Chamado #{protocolo} fechado pelo usuário {userEmail}");

            // ✅ Notificar via SignalR que o status mudou (Desktop/Mobile/Web)
            try
            {
                await _hubContext.Clients.Group($"ticket-{protocolo}")
                    .SendAsync("TicketStatusChanged", "Resolvido");
                
                _logger.LogInformation($"[FecharChamado] SignalR TicketStatusChanged enviado para ticket-{protocolo}");
                
                // ✅ Enviar evento específico de pesquisa para Web MVC
                await _hubContext.Clients.Group($"ticket-{protocolo}")
                    .SendAsync("ShowSatisfactionSurvey", protocolo.ToString(), chamado.UsuarioCriadorEmail);
                
                _logger.LogInformation($"[FecharChamado] SignalR ShowSatisfactionSurvey enviado para {chamado.UsuarioCriadorEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[FecharChamado] Erro ao enviar notificação SignalR: {ex.Message}");
            }

            return Ok(new { message = "Chamado fechado com sucesso" });
        }
    }

    // DTO para avaliação
    public class AvaliacaoDto
    {
        public int Rating { get; set; }
    }
}
