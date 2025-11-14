# 🎫 Sistema de Gestão de Chamados

Sistema completo de gerenciamento de tickets de suporte técnico com integração de chatbot inteligente usando OpenAI, desenvolvido em .NET com múltiplas plataformas (Web, Desktop e Mobile).

![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4?logo=.net)
![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)
![WPF](https://img.shields.io/badge/WPF-Desktop-0078D4?logo=windows)
![MAUI](https://img.shields.io/badge/.NET%20MAUI-Mobile-512BD4?logo=.net)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?logo=microsoftsqlserver)

## 📋 Sobre o Projeto

Sistema profissional de gestão de chamados de suporte técnico que permite:

- 🤖 **Chatbot Inteligente** com IA (OpenAI GPT-4) para atendimento automatizado
- 💬 **Chat em Tempo Real** entre clientes e técnicos via SignalR
- 👥 **Gestão de Usuários** com diferentes níveis de acesso (Cliente, Técnico, Gerente, Admin)
- 📊 **Dashboard Gerencial** com métricas e estatísticas
- 🎯 **Fila de Atendimento** para técnicos
- ⭐ **Sistema de Avaliação** de atendimento
- 🔐 **Autenticação Segura** com JWT e BCrypt
- 📱 **Multiplataforma** - Web, Desktop Windows e Mobile (Android/Windows)

## 🏗️ Arquitetura do Sistema

O projeto é dividido em múltiplos módulos:

```
GestaoChamados/
├── GestaoChamados.API/          # API REST (Backend principal)
├── GestaoChamados/              # Aplicação Web MVC
├── GestaoChamados.Desktop/      # Aplicativo Desktop WPF
├── GestaoChamados.Mobile/       # Aplicativo Mobile MAUI (Android/Windows)
├── GestaoChamados.Shared/       # Código compartilhado (DTOs, Services)
└── GestaoChamados.Tests/        # Testes unitários
```

### 🔄 Como Funciona

1. **Cliente** abre um chamado através do chatbot ou formulário
2. **Chatbot IA** tenta resolver automaticamente problemas simples
3. Se necessário, chamado entra na **fila de atendimento**
4. **Técnico** assume o chamado e atende via chat em tempo real
5. Após resolução, cliente **avalia o atendimento**
6. **Gerente** visualiza métricas e relatórios no dashboard

### 🎭 Perfis de Usuário

- **Cliente**: Abre chamados, conversa com chatbot, avalia atendimento
- **Técnico**: Atende fila, resolve chamados, chat ao vivo
- **Gerente**: Visualiza dashboards, cria usuários, relatórios gerenciais
- **Admin**: Acesso total ao sistema

## 🚀 Como Executar

### Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (para Mobile)
- [SQL Server](https://www.microsoft.com/sql-server) (ou SQL Server Express)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) ou [VS Code](https://code.visualstudio.com/)
- Chave API da [OpenAI](https://platform.openai.com/) (opcional, para chatbot)

### 📦 Configuração do Banco de Dados

1. **Criar o banco de dados:**
```bash
# Execute o script SQL
sqlcmd -S SEU_SERVIDOR -i Data/CREATE_DATABASE_COMPLETO.sql
```

Ou use o SQL Server Management Studio para executar `Data/CREATE_DATABASE_COMPLETO.sql`

2. **Configurar connection string:**

Edite `appsettings.json` nos projetos API e Web:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SEU_SERVIDOR;Database=GestaoChamadosDB;Integrated Security=true;TrustServerCertificate=True;"
  }
}
```

### 🔑 Configurar OpenAI (Opcional)

Em `GestaoChamados.API/appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "sua-api-key-aqui",
    "Model": "gpt-4o-mini",
    "BaseUrl": "https://api.openai.com/v1"
  }
}
```

### ▶️ Executar o Projeto

#### Opção 1: Visual Studio
1. Abra `GestaoChamados.sln`
2. Configure múltiplos projetos de inicialização:
   - `GestaoChamados.API` (primeira)
   - `GestaoChamados.Desktop` ou `GestaoChamados.Mobile`
3. Pressione `F5`

#### Opção 2: Linha de Comando

**API:**
```bash
cd GestaoChamados.API
dotnet run
# API rodará em: http://localhost:5142
```

**Desktop WPF:**
```bash
cd GestaoChamados.Desktop
dotnet run
```

**Mobile (Windows):**
```bash
cd GestaoChamados.Mobile
dotnet run -f net9.0-windows10.0.19041.0
```

**Mobile (Android):**
```bash
cd GestaoChamados.Mobile
dotnet build -f net9.0-android
# Deploy via emulador ou dispositivo físico
```

#### Opção 3: VS Code (Recomendado para Desenvolvimento)
1. Abra a pasta do projeto
2. Pressione `Ctrl+Shift+P` → "Tasks: Run Task"
3. Selecione `watch` (API com hot reload)

### 🔨 Gerar Builds de Produção

Use os scripts .bat incluídos:

```bash
# Build individual
Build-Desktop-WPF.bat          # Desktop executável
Build-Mobile-Android.bat       # APK para Android
Build-Mobile-Windows.bat       # Mobile Windows

# Build completo (tudo de uma vez)
Build-All.bat

# Limpar builds
Clean-All.bat
```

Arquivos gerados ficam em: `Scripts/`

## 🛠️ Tecnologias Utilizadas

### Backend
- **ASP.NET Core 8.0** - Framework web
- **Entity Framework Core** - ORM
- **SQL Server** - Banco de dados
- **SignalR** - Comunicação em tempo real
- **JWT** - Autenticação
- **BCrypt.Net** - Hash de senhas
- **OpenAI API** - Chatbot inteligente

### Frontend Web
- **ASP.NET Core MVC** - Padrão MVC
- **Razor Pages** - Views dinâmicas
- **Bootstrap 5** - UI Framework
- **jQuery** - Interatividade
- **SweetAlert2** - Alertas customizados

### Desktop
- **WPF (.NET 8.0)** - Windows Presentation Foundation
- **XAML** - Interface declarativa
- **MVVM Pattern** - Arquitetura

### Mobile
- **.NET MAUI 9.0** - Multi-platform App UI
- **XAML** - Interface
- **MVVM Pattern** - Arquitetura
- **Android SDK** - Plataforma Android

### DevOps & Ferramentas
- **Git** - Controle de versão
- **VS Code** - Editor de código
- **Visual Studio 2022** - IDE
- **Postman/HTTP Files** - Testes de API

## 📁 Estrutura do Projeto

```
GestaoChamados/
│
├── 📂 GestaoChamados.API/              # API REST
│   ├── Controllers/                     # Endpoints da API
│   ├── Data/                           # DbContext
│   ├── Hubs/                           # SignalR Hubs
│   ├── Models/                         # Entidades
│   ├── Services/                       # Lógica de negócio
│   └── appsettings.json                # Configurações
│
├── 📂 GestaoChamados/                  # Web MVC
│   ├── Controllers/                     # Controllers MVC
│   ├── Views/                          # Views Razor
│   ├── wwwroot/                        # Assets estáticos
│   └── Services/                       # Serviços
│
├── 📂 GestaoChamados.Desktop/          # Desktop WPF
│   ├── Views/                          # Janelas XAML
│   ├── ViewModels/                     # View Models
│   └── Services/                       # Serviços
│
├── 📂 GestaoChamados.Mobile/           # Mobile MAUI
│   ├── Views/                          # Pages XAML
│   ├── ViewModels/                     # View Models
│   ├── Services/                       # Serviços
│   └── Platforms/                      # Código específico por plataforma
│
├── 📂 GestaoChamados.Shared/           # Código Compartilhado
│   ├── DTOs/                           # Data Transfer Objects
│   └── Services/                       # ApiService compartilhado
│
├── 📂 Data/                            # Scripts SQL
│   └── CREATE_DATABASE_COMPLETO.sql    # Criação do banco
│
├── 📂 Scripts/                         # Builds gerados
│
└── 📄 GestaoChamados.sln               # Solution file

```

## 🔐 Usuários Padrão

Após executar o script SQL, os seguintes usuários estarão disponíveis:

| Email | Senha | Perfil |
|-------|-------|--------|
| gerente@empresa.com | senha123 | Gerente |
| admin@empresa.com | senha123 | Admin |
| tecnico@empresa.com | senha123 | Técnico |
| cliente@empresa.com | senha123 | Cliente |

⚠️ **Altere as senhas em produção!**

## 🎯 Funcionalidades Principais

### Chatbot Inteligente
- ✅ Respostas automáticas usando GPT-4
- ✅ Detecção de problemas de TI
- ✅ Escalonamento inteligente para atendimento humano
- ✅ Histórico de conversação

### Chat em Tempo Real
- ✅ SignalR para comunicação instantânea
- ✅ Notificações push
- ✅ Histórico persistente
- ✅ Indicador de digitação

### Dashboard Gerencial
- ✅ Métricas de atendimento
- ✅ Taxa de resolução
- ✅ Tempo médio de atendimento
- ✅ Avaliações de satisfação
- ✅ Gráficos e estatísticas

### Sistema de Fila
- ✅ Fila FIFO para chamados
- ✅ Atribuição automática
- ✅ Notificações em tempo real
- ✅ Priorização de chamados


## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para:

1. Fork o projeto
2. Criar uma branch para sua feature (`git checkout -b feature/NovaFuncionalidade`)
3. Commit suas mudanças (`git commit -m 'Adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feature/NovaFuncionalidade`)
5. Abrir um Pull Request

## 📝 Licença

Este projeto está sob a licença MIT. Veja o arquivo `LICENSE` para mais detalhes.


---

⭐ Se este projeto foi útil, considere dar uma estrela no GitHub!

## 📞 Suporte

Para dúvidas e suporte:
- Abra uma [Issue](../../issues)
- Consulte a [documentação](../../wiki)

---
