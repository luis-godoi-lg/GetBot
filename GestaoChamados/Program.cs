using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using GestaoChamados.Data;
using GestaoChamados.Hubs;
using GestaoChamados.Services;
using System.Security.Claims;

// Configuração de encoding UTF-8 global para suportar caracteres especiais portugueses
Console.OutputEncoding = System.Text.Encoding.UTF8;
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Configuração de logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

// --- CONFIGURAÇÃO DO ENTITY FRAMEWORK E SQL SERVER ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- CONFIGURAÇÃO DE AUTENTICAÇÃO JWT ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ChaveSecretaSuperSeguraDeNoMinimo32Caracteres123456";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "GestaoChamadosAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "GestaoChamadosClients";

builder.Services.AddAuthentication(options =>
{
    // Para MVC (Views) usa Cookie como padrão
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Para SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/supportHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Login/Index";
    options.AccessDeniedPath = "/Login/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.Cookie.Name = "GestaoChamados.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Permite HTTP em desenvolvimento
    options.Cookie.SameSite = SameSiteMode.Lax; // Menos restritivo para desenvolvimento
});

// Configura��o de Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Limite global por IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "an�nimo",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Limite espec�fico para login
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "an�nimo",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(15)
            }));
});

// --- CONFIGURAÇÃO DE SEGURANÇA E SERVIÇOS ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping; // Permite caracteres especiais UTF-8
    });

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();

// --- CONFIGURAÇÃO DO SWAGGER ---
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Gestão de Chamados API",
        Version = "v1",
        Description = "API REST para sistema de gestão de chamados (tickets) - Suporta Web e Desktop",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Suporte",
            Email = "suporte@gestaochamados.com"
        }
    });

    // Configuração de autenticação JWT no Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Insira o token JWT no formato: Bearer {seu token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddSignalR();

// --- CONFIGURAÇÃO DE CORS PARA CLIENTES EXTERNOS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("AllowSpecific", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",  // React
                "http://localhost:5173",  // Vite
                "http://localhost:8080",  // Vue
                "http://localhost:4200"   // Angular
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Cache e sess�o com configura��es de seguran�a
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Permite HTTP em desenvolvimento
    options.Cookie.SameSite = SameSiteMode.Lax; // Menos restritivo para desenvolvimento
});

// Servi�os HTTP e Chatbot
builder.Services.AddHttpClient<IChatbotService, OpenAIChatbotService>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

// IMPORTANT: use the typed HttpClient registration above. Do NOT register a second
// scoped registration for the same interface/implementation because it will
// override the IHttpClientFactory configured client and may produce an HttpClient
// without the expected handler/configuration.

// Validate OpenAI configuration early and warn if the ApiKey looks missing/placeholder
var openAiKeyCheck = builder.Configuration["OpenAI:ApiKey"];
if (string.IsNullOrWhiteSpace(openAiKeyCheck) || openAiKeyCheck.Contains("sua-api-key") || openAiKeyCheck.Contains("proj-demo"))
{
    // Use Console for startup visibility in simple deployments; logging isn't fully built yet.
    Console.WriteLine("[Startup] WARNING: OpenAI:ApiKey is missing or appears to be a placeholder. Verify your OpenAI configuration (appsettings or environment variable).");
}

// Serviço HTTP para consumir a API
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ApiService>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var baseUrl = config["ApiSettings:BaseUrl"] ?? "http://localhost:5142";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });
builder.Services.AddScoped<ApiService>();

var app = builder.Build();

// --- PIPELINE DE REQUISIÇÃO ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// --- HABILITAR SWAGGER ---
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestão de Chamados API v1");
    options.RoutePrefix = "swagger"; // Acesse em: http://localhost:5013/swagger
    options.DocumentTitle = "Gestão de Chamados - API Documentation";
});

// Middleware de segurança
// Comentado para permitir HTTP em desenvolvimento
// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// --- HABILITAR CORS ---
app.UseCors("AllowSpecific");

// Headers de segurança
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseSession();
app.UseRateLimiter();

// Middleware de diagnóstico de autenticação
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("========== AUTH DEBUG ==========");
    logger.LogInformation($"Path: {context.Request.Path}");
    logger.LogInformation($"User.Identity.IsAuthenticated: {context.User?.Identity?.IsAuthenticated}");
    logger.LogInformation($"User.Identity.Name: {context.User?.Identity?.Name}");
    
    // Verificar cookies
    if (context.Request.Cookies.Any())
    {
        logger.LogInformation("Cookies recebidos:");
        foreach (var cookie in context.Request.Cookies)
        {
            logger.LogInformation($"  - {cookie.Key}: {cookie.Value.Substring(0, Math.Min(20, cookie.Value.Length))}...");
        }
    }
    else
    {
        logger.LogWarning("NENHUM COOKIE RECEBIDO!");
    }
    
    // Verificar session
    if (context.Session.IsAvailable)
    {
        logger.LogInformation("Session disponível:");
        logger.LogInformation($"  - UserEmail: {context.Session.GetString("UserEmail")}");
        logger.LogInformation($"  - UserName: {context.Session.GetString("UserName")}");
        logger.LogInformation($"  - UserRole: {context.Session.GetString("UserRole")}");
    }
    else
    {
        logger.LogWarning("SESSION NÃO DISPONÍVEL!");
    }
    
    logger.LogInformation("================================");
    
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// ==================== SEEDER DE USUÁRIOS PADRÃO ====================
// Cria um usuário Gerente padrão se não existir
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    SeedDefaultUsers(serviceProvider);
}

// Rotas e Hubs
app.MapControllers(); // API REST

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.MapHub<SupportHub>("/supportHub")
    .RequireAuthorization(); // Requer autenticação para WebSocket

app.Run();

// ==================== MÉTODO SEEDER ====================
static void SeedDefaultUsers(IServiceProvider serviceProvider)
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("[Startup] Verificando usuários padrão...");
        
        using (var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>())
        {
            // Verificar se o gerente já existe
            var gerenteExistente = dbContext.Usuarios
                .FirstOrDefault(u => u.Email == "gerente@sistema.com");
            
            if (gerenteExistente == null)
            {
                // Senha em texto plano (sem criptografia)
                var usuario = new GestaoChamados.Models.UsuarioModel
                {
                    Nome = "Gerente do Sistema",
                    Email = "gerente@sistema.com",
                    Senha = "senha123",
                    Role = "Gerente",
                    DataCriacao = DateTime.Now
                };

                dbContext.Usuarios.Add(usuario);
                dbContext.SaveChanges();

                logger.LogInformation("[Startup] ✅ Usuário Gerente criado com sucesso!");
                logger.LogInformation("[Startup] 👨‍💼 Email: gerente@sistema.com | Senha: senha123");
            }
            else
            {
                logger.LogInformation("[Startup] ✅ Usuário Gerente já existe no banco de dados");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Startup] ❌ Erro ao criar usuários padrão");
    }
}

// Funções de hash removidas - sistema agora usa senhas em texto plano
