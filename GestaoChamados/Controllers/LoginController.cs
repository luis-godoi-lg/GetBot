using GestaoChamados.Models;
using GestaoChamados.Services;
using GestaoChamados.Shared.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;

namespace GestaoChamados.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly ApiService _apiService;
        private readonly Dictionary<string, (int attempts, DateTime lastAttempt)> _loginAttempts = new();
        private const int MaxLoginAttempts = 5;
        private const int LockoutMinutes = 15;

        public LoginController(ILogger<LoginController> logger, ApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }
        public async Task<IActionResult> Index()
        {
            // Se o usuário já está autenticado, mostra a tela de login mesmo assim
            // (permite que ele faça logout ou troque de conta)
            // Não redireciona automaticamente para evitar confusão
            
            // Se quiser forçar logout ao acessar a página de login, descomente:
            if (User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("Usuário já autenticado acessando tela de login: {Email}. Fazendo logout automático.", User.Identity.Name);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            return View();
        }

        // POST: Recebe os dados do formulário e tenta autenticar
        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verifica rate limiting
            if (IsUserLockedOut(model.Email))
            {
                ModelState.AddModelError(string.Empty, 
                    "Conta temporariamente bloqueada por excesso de tentativas. Tente novamente mais tarde.");
                _logger.LogWarning("Tentativa de login durante bloqueio: {Email}", model.Email);
                return View(model);
            }

            try
            {
                // Chama a API para fazer login
                var loginRequest = new LoginRequestDto
                {
                    Email = model.Email,
                    Senha = model.Password
                };

                var response = await _apiService.PostAsync<LoginRequestDto>("/api/auth/login", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (loginResponse != null)
                    {
                        // LOG: Debug do que veio da API
                        _logger.LogInformation("=== RESPOSTA DA API DE LOGIN ===");
                        _logger.LogInformation("Email: {Email}", loginResponse.Email);
                        _logger.LogInformation("Nome: {Nome}", loginResponse.Nome);
                        _logger.LogInformation("Role: {Role}", loginResponse.Role);
                        _logger.LogInformation("Token: {Token}", loginResponse.Token?.Substring(0, Math.Min(50, loginResponse.Token?.Length ?? 0)) + "...");
                        _logger.LogInformation("================================");

                        // Limpa as tentativas de login após sucesso
                        ResetLoginAttempts(model.Email);

                        // Salva o token JWT na sessão
                        if (!string.IsNullOrEmpty(loginResponse.Token))
                            HttpContext.Session.SetString("JwtToken", loginResponse.Token);
                        
                        if (!string.IsNullOrEmpty(loginResponse.Email))
                            HttpContext.Session.SetString("UserEmail", loginResponse.Email);
                        
                        if (!string.IsNullOrEmpty(loginResponse.Nome))
                            HttpContext.Session.SetString("UserName", loginResponse.Nome);
                        
                        if (!string.IsNullOrEmpty(loginResponse.Role))
                            HttpContext.Session.SetString("UserRole", loginResponse.Role);

                        // Cria os claims para o Cookie de autenticação
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, loginResponse.Email),
                            new Claim(ClaimTypes.GivenName, loginResponse.Nome),
                            new Claim(ClaimTypes.Role, loginResponse.Role)
                        };

                        _logger.LogInformation("=== CLAIMS CRIADOS ===");
                        foreach (var claim in claims)
                        {
                            _logger.LogInformation("Claim -> Type: {Type}, Value: {Value}", claim.Type, claim.Value);
                        }
                        _logger.LogInformation("======================");

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        _logger.LogInformation("Login bem-sucedido via API para o usuário: {Email}", model.Email);

                        // Redireciona baseado no papel
                        if (loginResponse.Role == "Gerente")
                            return RedirectToAction("Dashboard", "Manager");
                        else if (loginResponse.Role == "Tecnico")
                            return RedirectToAction("Index", "Dashboard");
                        else
                            return RedirectToAction("Index", "Chamado");
                    }
                }
                else
                {
                    // Lê o erro retornado pela API
                    var errorContent = await response.Content.ReadAsStringAsync();
                    string errorMessage = "Email ou senha inválidos.";

                    try
                    {
                        // Tenta extrair a mensagem de erro do JSON
                        var errorJson = JsonDocument.Parse(errorContent);
                        if (errorJson.RootElement.TryGetProperty("message", out var messageElement))
                        {
                            errorMessage = messageElement.GetString() ?? errorMessage;
                        }
                        else if (errorJson.RootElement.TryGetProperty("title", out var titleElement))
                        {
                            errorMessage = titleElement.GetString() ?? errorMessage;
                        }
                        else
                        {
                            // Se for um texto simples, usa diretamente
                            errorMessage = !string.IsNullOrWhiteSpace(errorContent) ? errorContent : errorMessage;
                        }
                    }
                    catch
                    {
                        // Se não conseguir fazer parse do JSON, usa o conteúdo como texto
                        errorMessage = !string.IsNullOrWhiteSpace(errorContent) && errorContent.Length < 200 
                            ? errorContent 
                            : "Email ou senha inválidos.";
                    }

                    // Registra tentativa falha
                    RecordFailedAttempt(model.Email);
                    
                    ModelState.AddModelError(string.Empty, errorMessage);
                    _logger.LogWarning("Tentativa de login falha via API para: {Email}. Erro: {Error}", model.Email, errorMessage);
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Erro de conexão ao tentar fazer login via API para: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Não foi possível conectar com o servidor de autenticação. Verifique se a API está rodando.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao tentar fazer login via API para: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Erro ao processar o login. Tente novamente mais tarde.");
            }
            
            return View(model);
        }

        // Action para fazer Logout
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            _logger.LogInformation("Logout realizado para o usuário: {Email}", userName);
            
            return RedirectToAction("Index", "Login");
        }

        #region Helper Methods

        private bool IsUserLockedOut(string email)
        {
            if (!_loginAttempts.ContainsKey(email))
                return false;

            var (attempts, lastAttempt) = _loginAttempts[email];
            return attempts >= MaxLoginAttempts && 
                   DateTime.UtcNow.Subtract(lastAttempt).TotalMinutes < LockoutMinutes;
        }

        private void RecordFailedAttempt(string email)
        {
            if (!_loginAttempts.ContainsKey(email))
            {
                _loginAttempts[email] = (1, DateTime.UtcNow);
            }
            else
            {
                var (attempts, _) = _loginAttempts[email];
                _loginAttempts[email] = (attempts + 1, DateTime.UtcNow);

                if (attempts + 1 >= MaxLoginAttempts)
                {
                    _logger.LogWarning("Conta bloqueada por excesso de tentativas: {Email}", email);
                }
            }
        }

        private void ResetLoginAttempts(string email)
        {
            if (_loginAttempts.ContainsKey(email))
            {
                _loginAttempts.Remove(email);
            }
        }

        #endregion
    }
}