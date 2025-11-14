using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestaoChamados.Data;
using GestaoChamados.DTOs;
using GestaoChamados.Models;

namespace GestaoChamados.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(
            ApplicationDbContext context,
            ILogger<UsuariosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lista todos os usuários (apenas técnico)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Tecnico")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new
                {
                    u.Id,
                    u.Nome,
                    u.Email,
                    u.Role
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        /// <summary>
        /// Busca usuário por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
                return NotFound(new { message = "Usuário não encontrado" });

            return Ok(new
            {
                usuario.Id,
                usuario.Nome,
                usuario.Email,
                usuario.Role
            });
        }

        /// <summary>
        /// Atualiza usuário
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Tecnico")]
        public async Task<IActionResult> UpdateUsuario(int id, [FromBody] CriarUsuarioDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound(new { message = "Usuário não encontrado" });

            usuario.Nome = request.Nome;
            usuario.Email = request.Email;
            usuario.Role = request.Role;

            if (!string.IsNullOrEmpty(request.Senha))
                usuario.Senha = request.Senha; // TODO: hash

            _context.Entry(usuario).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Exclui usuário (apenas técnico)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Tecnico")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound(new { message = "Usuário não encontrado" });

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
