using GestaoChamados.Models;
using GestaoChamados.Services;
using GestaoChamados.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestaoChamados.Controllers
{
    [Authorize(Roles = "Gerente")] // Apenas Gerente pode acessar esta área
    public class UsuarioController : Controller
    {
        private readonly ApiService _apiService;

        public UsuarioController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // Action para exibir a lista de todos os usuários
        public async Task<IActionResult> Index()
        {
            try
            {
                var usuarios = await _apiService.GetAsync<List<UsuarioModel>>("/api/usuarios");
                return View(usuarios ?? new List<UsuarioModel>());
            }
            catch
            {
                TempData["ErrorMessage"] = "Erro ao carregar lista de usuários.";
                return View(new List<UsuarioModel>());
            }
        }

        // Action para exibir o formulário de criação (GET)
        public IActionResult Criar()
        {
            return View();
        }

        // Action para processar os dados do formulário (POST)
        [HttpPost]
        public async Task<IActionResult> Criar(CriarUsuarioViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dto = new CriarEditarUsuarioDto
                    {
                        Nome = model.Nome,
                        Email = model.Email,
                        Senha = model.Senha,
                        Role = model.Role
                    };

                    var response = await _apiService.PostAsync<CriarEditarUsuarioDto>("/api/auth/register", dto);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Usuário cadastrado com sucesso!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        ModelState.AddModelError(string.Empty, $"Erro ao criar usuário: {errorContent}");
                    }
                }
                catch (System.Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Erro ao conectar com a API: {ex.Message}");
                }
            }

            return View(model);
        }
    }
}