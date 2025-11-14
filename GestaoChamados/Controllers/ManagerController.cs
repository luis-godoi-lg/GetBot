using GestaoChamados.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestaoChamados.Services;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.Controllers
{
    [Authorize(Roles = "Gerente")]
    public class ManagerController : Controller
    {
        private readonly ApiService _apiService;

        public ManagerController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var dashboardDto = await _apiService.GetAsync<ManagerDashboardDto>("/api/manager/dashboard");
                
                if (dashboardDto == null)
                {
                    return View(new ManagerDashboardDto());
                }
                
                return View(dashboardDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ManagerController] Erro ao buscar dashboard: {ex.Message}");
                return View(new ManagerDashboardDto());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GerenciarUsuarios()
        {
            try
            {
                var usuarios = await _apiService.GetAsync<List<ListarUsuarioDto>>("/api/manager/usuarios");
                
                if (usuarios == null)
                {
                    return View(new List<ListarUsuarioDto>());
                }
                
                return View(usuarios);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ManagerController] Erro ao buscar usuários: {ex.Message}");
                return View(new List<ListarUsuarioDto>());
            }
        }

        [HttpGet]
        public IActionResult CriarUsuario()
        {
            var dto = new CriarEditarUsuarioDto();
            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> CriarUsuario(CriarEditarUsuarioDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Senha) || string.IsNullOrWhiteSpace(dto.Role))
            {
                TempData["ErrorMessage"] = "Todos os campos são obrigatórios.";
                return RedirectToAction(nameof(CriarUsuario));
            }

            // Validar role
            if (!new[] { "Usuario", "Tecnico", "Gerente" }.Contains(dto.Role))
            {
                TempData["ErrorMessage"] = "Papel inválido.";
                return RedirectToAction(nameof(CriarUsuario));
            }

            try
            {
                var response = await _apiService.PostAsync<CriarEditarUsuarioDto, ListarUsuarioDto>("/api/manager/usuarios", dto);
                
                if (response != null)
                {
                    TempData["SuccessMessage"] = $"Usuário '{dto.Nome}' criado com sucesso!";
                    return RedirectToAction(nameof(GerenciarUsuarios));
                }
                
                TempData["ErrorMessage"] = "Erro ao criar usuário na API.";
                return RedirectToAction(nameof(CriarUsuario));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao criar usuário: {ex.Message}";
                return RedirectToAction(nameof(CriarUsuario));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            try
            {
                var usuarios = await _apiService.GetAsync<List<ListarUsuarioDto>>("/api/manager/usuarios");
                var usuario = usuarios?.FirstOrDefault(u => u.Id == id);
                
                if (usuario == null)
                {
                    return NotFound();
                }

                var dto = new CriarEditarUsuarioDto
                {
                    Id = usuario.Id,
                    Nome = usuario.Nome,
                    Email = usuario.Email,
                    Role = usuario.Role
                };

                return View(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ManagerController] Erro ao buscar usuário: {ex.Message}");
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditarUsuario(int id, CriarEditarUsuarioDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Role))
            {
                TempData["ErrorMessage"] = "Todos os campos são obrigatórios.";
                return RedirectToAction(nameof(EditarUsuario), new { id });
            }

            try
            {
                var response = await _apiService.PutAsync($"/api/manager/usuarios/{id}", dto);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = $"Usuário '{dto.Nome}' atualizado com sucesso!";
                    return RedirectToAction(nameof(GerenciarUsuarios));
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = $"Erro ao atualizar usuário: {errorContent}";
                return RedirectToAction(nameof(EditarUsuario), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao atualizar usuário: {ex.Message}";
                return RedirectToAction(nameof(EditarUsuario), new { id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletarUsuario([FromBody] Dictionary<string, int> data)
        {
            if (data == null || !data.ContainsKey("id"))
            {
                return Json(new { success = false, message = "ID do usuário não fornecido." });
            }

            int id = data["id"];
            
            try
            {
                await _apiService.DeleteAsync($"/api/manager/usuarios/{id}");
                return Json(new { success = true, message = "Usuário deletado com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao deletar usuário: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> RelatorioDetalhado()
        {
            try
            {
                var relatorio = await _apiService.GetAsync<RelatorioDetalhadoDto>("/api/manager/relatorio");
                
                if (relatorio == null)
                {
                    return View(new RelatorioDetalhadoDto());
                }
                
                return View(relatorio);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ManagerController] Erro ao buscar relatório: {ex.Message}");
                return View(new RelatorioDetalhadoDto());
            }
        }
    }
}