using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestaoChamados.Data;
using GestaoChamados.DTOs;
using GestaoChamados.Models;
using BCrypt.Net;

namespace GestaoChamados.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Realiza login e retorna token JWT
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.Senha))
            {
                _logger.LogWarning($"Tentativa de login falhou para {request.Email}");
                return Unauthorized(new { message = "Email ou senha inválidos" });
            }

            var token = GenerateJwtToken(usuario);
            var expiresAt = DateTime.UtcNow.AddHours(8);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Email = usuario.Email,
                Nome = usuario.Nome,
                Role = usuario.Role,
                ExpiresAt = expiresAt
            });
        }

        /// <summary>
        /// Registra novo usuário (apenas Gerente pode criar)
        /// </summary>
        [HttpPost("register")]
        [Authorize(Roles = "Gerente")]
        public async Task<ActionResult<LoginResponseDto>> Register([FromBody] CriarUsuarioDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verifica se email já existe
            if (await _context.Usuarios.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Email já cadastrado" });
            }

            var usuario = new UsuarioModel
            {
                Nome = request.Nome,
                Email = request.Email,
                Senha = BCrypt.Net.BCrypt.HashPassword(request.Senha), // Senha criptografada com BCrypt
                Role = request.Role
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(usuario);
            var expiresAt = DateTime.UtcNow.AddHours(8);

            return CreatedAtAction(nameof(Register), new LoginResponseDto
            {
                Token = token,
                Email = usuario.Email,
                Nome = usuario.Nome,
                Role = usuario.Role,
                ExpiresAt = expiresAt
            });
        }

        /// <summary>
        /// Verifica se token é válido
        /// </summary>
        [HttpGet("verify")]
        [Authorize]
        public IActionResult VerifyToken()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new { 
                valid = true, 
                email = email, 
                role = role 
            });
        }

        private string GenerateJwtToken(UsuarioModel usuario)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "ChaveSecretaSuperSeguraDeNoMinimo32Caracteres123456"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Role, usuario.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "GestaoChamadosAPI",
                audience: _configuration["Jwt:Audience"] ?? "GestaoChamadosClients",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
