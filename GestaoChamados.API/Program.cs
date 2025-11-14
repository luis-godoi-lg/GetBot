/// <summary>
/// Program.cs - Ponto de entrada principal da API de Gestão de Chamados
/// Configura todos os serviços, middleware e endpoints da aplicação
/// </summary>

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using GestaoChamados.Data;
using GestaoChamados.Hubs;
using GestaoChamados.Services;

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
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
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
});

// Configuração de Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Limite global por IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anônimo",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Limite específico para login
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anônimo",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(15)
            }));
});

// --- CONFIGURAÇÃO DE SERVIÇOS ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

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
        policy.WithOrigins(
                "http://localhost:5013",   // Web MVC
                "https://localhost:7027",  // Web MVC (HTTPS)
                "http://localhost:63933",  // Web MVC (IIS Express)
                "http://localhost:3000",   // React
                "http://localhost:5173",   // Vite
                "http://localhost:8080",   // Vue
                "http://localhost:4200"    // Angular
            )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Necessário para SignalR com autenticação
    });

    options.AddPolicy("AllowSpecific", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5013",   // Web MVC
                "https://localhost:7027",  // Web MVC (HTTPS)
                "http://localhost:63933",  // Web MVC (IIS Express)
                "http://localhost:3000",   // React
                "http://localhost:5173",   // Vite
                "http://localhost:8080",   // Vue
                "http://localhost:4200"    // Angular
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Serviços HTTP e Chatbot
builder.Services.AddHttpClient<IChatbotService, OpenAIChatbotService>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddScoped<IChatbotService, OpenAIChatbotService>();

var app = builder.Build();

// --- PIPELINE DE REQUISIÇÃO ---
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// --- HABILITAR SWAGGER ---
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestão de Chamados API v1");
    options.RoutePrefix = "swagger"; // Acesse em: http://localhost:5014/swagger
    options.DocumentTitle = "Gestão de Chamados - API Documentation";
});

// Middleware de segurança
app.UseHttpsRedirection();
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

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Rotas e Hubs
app.MapControllers(); // API REST

app.MapHub<SupportHub>("/supportHub")
    .RequireAuthorization(); // Requer autenticação para WebSocket

app.Run();
