-- =====================================================
-- SCRIPT COMPLETO DE CRIAÇÃO DO BANCO DE DADOS
-- Sistema: Gestão de Chamados
-- Banco: GestaoChamadosDB
-- SQL Server / LocalDB
-- Data: 08/11/2025
-- =====================================================
-- 
-- AUDITORIA BASEADA EM:
-- - Models: UsuarioModel, ChamadoModel, ChatMessageModel
-- - ApplicationDbContext.cs
-- - Senhas: Texto plano (compatível com sistema atual)
-- - Usuários padrão encontrados na documentação
--
-- =====================================================

USE master;
GO

-- =====================================================
-- PASSO 1: CRIAR BANCO DE DADOS
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'GestaoChamadosDB')
BEGIN
    CREATE DATABASE GestaoChamadosDB;
    PRINT '✅ Banco de dados GestaoChamadosDB criado com sucesso!';
END
ELSE
BEGIN
    PRINT '⚠️  Banco de dados GestaoChamadosDB já existe.';
END
GO

USE GestaoChamadosDB;
GO

-- =====================================================
-- PASSO 2: CRIAR TABELA DE USUÁRIOS
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Usuarios')
BEGIN
    CREATE TABLE Usuarios (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(200) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        Senha NVARCHAR(500) NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        DataCriacao DATETIME2 NOT NULL DEFAULT GETDATE(),
        
        -- Constraints
        CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
        CONSTRAINT CK_Usuarios_Role CHECK (Role IN ('Usuario', 'Tecnico', 'Gerente'))
    );
    
    PRINT '✅ Tabela Usuarios criada com sucesso!';
END
ELSE
BEGIN
    PRINT '⚠️  Tabela Usuarios já existe.';
END
GO

-- =====================================================
-- PASSO 3: CRIAR TABELA DE CHAMADOS
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Chamados')
BEGIN
    CREATE TABLE Chamados (
        Protocolo INT IDENTITY(1,1) PRIMARY KEY,
        Assunto NVARCHAR(300) NOT NULL,
        Descricao NVARCHAR(2000) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Aberto',
        DataAbertura DATETIME2 NOT NULL DEFAULT GETDATE(),
        UsuarioCriadorEmail NVARCHAR(200) NOT NULL,
        TecnicoAtribuidoEmail NVARCHAR(200) NULL,
        AnexoNomeArquivo NVARCHAR(500) NULL,
        Rating INT NULL,
        
        -- Constraints
        CONSTRAINT FK_Chamados_UsuarioCriador FOREIGN KEY (UsuarioCriadorEmail) 
            REFERENCES Usuarios(Email) ON UPDATE CASCADE,
        CONSTRAINT CK_Chamados_Rating CHECK (Rating >= 1 AND Rating <= 5)
    );
    
    PRINT '✅ Tabela Chamados criada com sucesso!';
END
ELSE
BEGIN
    PRINT '⚠️  Tabela Chamados já existe.';
END
GO

-- =====================================================
-- PASSO 4: CRIAR TABELA DE MENSAGENS DE CHAT
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatMessages')
BEGIN
    CREATE TABLE ChatMessages (
        TicketId INT NOT NULL,
        Timestamp DATETIME2 NOT NULL,
        SenderEmail NVARCHAR(200) NOT NULL,
        SenderName NVARCHAR(200) NOT NULL,
        MessageText NVARCHAR(2000) NOT NULL,
        
        -- Chave primária composta
        CONSTRAINT PK_ChatMessages PRIMARY KEY (TicketId, Timestamp),
        
        -- Foreign key
        CONSTRAINT FK_ChatMessages_Chamado FOREIGN KEY (TicketId) 
            REFERENCES Chamados(Protocolo) ON DELETE CASCADE
    );
    
    PRINT '✅ Tabela ChatMessages criada com sucesso!';
END
ELSE
BEGIN
    PRINT '⚠️  Tabela ChatMessages já existe.';
END
GO

-- =====================================================
-- PASSO 5: CRIAR ÍNDICES PARA PERFORMANCE
-- =====================================================

-- Índices na tabela Chamados
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Chamados_Status' AND object_id = OBJECT_ID('Chamados'))
    CREATE INDEX IX_Chamados_Status ON Chamados(Status);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Chamados_UsuarioCriadorEmail' AND object_id = OBJECT_ID('Chamados'))
    CREATE INDEX IX_Chamados_UsuarioCriadorEmail ON Chamados(UsuarioCriadorEmail);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Chamados_TecnicoAtribuidoEmail' AND object_id = OBJECT_ID('Chamados'))
    CREATE INDEX IX_Chamados_TecnicoAtribuidoEmail ON Chamados(TecnicoAtribuidoEmail);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Chamados_DataAbertura' AND object_id = OBJECT_ID('Chamados'))
    CREATE INDEX IX_Chamados_DataAbertura ON Chamados(DataAbertura DESC);

-- Índices na tabela ChatMessages
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChatMessages_TicketId' AND object_id = OBJECT_ID('ChatMessages'))
    CREATE INDEX IX_ChatMessages_TicketId ON ChatMessages(TicketId);

PRINT '✅ Índices criados com sucesso!';
GO

-- =====================================================
-- PASSO 6: INSERIR USUÁRIOS PADRÃO (SEED DATA)
-- =====================================================

PRINT '';
PRINT '========================================';
PRINT 'INSERINDO USUÁRIOS PADRÃO...';
PRINT '========================================';

-- 1. Administrador do Sistema
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'admin@gestaochamados.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role, DataCriacao)
    VALUES ('Administrador', 'admin@gestaochamados.com', 'admin123', 'Tecnico', GETDATE());
    PRINT '✅ Usuário criado: admin@gestaochamados.com (Técnico)';
END

-- 2. Gerente do Sistema
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'gerente@sistema.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role, DataCriacao)
    VALUES ('Gerente do Sistema', 'gerente@sistema.com', 'senha123', 'Gerente', GETDATE());
    PRINT '✅ Usuário criado: gerente@sistema.com (Gerente)';
END

-- 3. Gerente alternativo
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'gerente@email.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role, DataCriacao)
    VALUES ('Gerente da Silva', 'gerente@email.com', '123456', 'Gerente', GETDATE());
    PRINT '✅ Usuário criado: gerente@email.com (Gerente)';
END

-- 4. Técnico Principal
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'tecnico@email.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role, DataCriacao)
    VALUES ('Técnico da Silva', 'tecnico@email.com', '123456', 'Tecnico', GETDATE());
    PRINT '✅ Usuário criado: tecnico@email.com (Técnico)';
END

-- 5. Técnico para Teste
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'tecnico@teste.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role, DataCriacao)
    VALUES ('Técnico de Teste', 'tecnico@teste.com', 'Tecnico@123', 'Tecnico', GETDATE());
    PRINT '✅ Usuário criado: tecnico@teste.com (Técnico)';
END

-- 6. Usuário Comum Principal
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'usuario@email.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role, DataCriacao)
    VALUES ('Usuário da Silva', 'usuario@email.com', '123456', 'Usuario', GETDATE());
    PRINT '✅ Usuário criado: usuario@email.com (Usuário)';
END

-- 7. Usuário de Teste 1
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'usuario@teste.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role, DataCriacao)
    VALUES ('Usuário Teste', 'usuario@teste.com', 'user123', 'Usuario', GETDATE());
    PRINT '✅ Usuário criado: usuario@teste.com (Usuário)';
END

-- 8. Usuário de Teste 2
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'usuario@teste2.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role, DataCriacao)
    VALUES ('Usuário Teste 2', 'usuario@teste2.com', 'Usuario@123', 'Usuario', GETDATE());
    PRINT '✅ Usuário criado: usuario@teste2.com (Usuário)';
END

-- 9. Usuário Jumento (encontrado nos testes)
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'jumento@gmail.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role, DataCriacao)
    VALUES ('Jumento de Teste', 'jumento@gmail.com', 'senha123', 'Usuario', GETDATE());
    PRINT '✅ Usuário criado: jumento@gmail.com (Usuário)';
END

-- 10. Usuário para teste BCrypt
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'teste@bcrypt.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role, DataCriacao)
    VALUES ('Teste BCrypt', 'teste@bcrypt.com', '123456', 'Usuario', GETDATE());
    PRINT '✅ Usuário criado: teste@bcrypt.com (Usuário)';
END

PRINT '';
PRINT '========================================';
PRINT 'USUÁRIOS CRIADOS COM SUCESSO!';
PRINT '========================================';
GO

-- =====================================================
-- PASSO 7: INSERIR CHAMADOS DE EXEMPLO
-- =====================================================

PRINT '';
PRINT '========================================';
PRINT 'INSERINDO CHAMADOS DE EXEMPLO...';
PRINT '========================================';

-- Chamado 1: Aberto
IF NOT EXISTS (SELECT * FROM Chamados WHERE Assunto = 'Problema com acesso ao sistema')
BEGIN
    INSERT INTO Chamados (Assunto, Descricao, Status, DataAbertura, UsuarioCriadorEmail, TecnicoAtribuidoEmail, Rating)
    VALUES (
        'Problema com acesso ao sistema',
        'Não consigo fazer login no sistema desde ontem. Tentei redefinir a senha mas não recebi o e-mail.',
        'Aberto',
        GETDATE(),
        'usuario@teste.com',
        NULL,
        NULL
    );
    PRINT '✅ Chamado criado: Problema com acesso ao sistema';
END

-- Chamado 2: Em Atendimento
IF NOT EXISTS (SELECT * FROM Chamados WHERE Assunto = 'Computador não liga')
BEGIN
    INSERT INTO Chamados (Assunto, Descricao, Status, DataAbertura, UsuarioCriadorEmail, TecnicoAtribuidoEmail, Rating)
    VALUES (
        'Computador não liga',
        'O computador da recepção não está ligando. Já verificamos a tomada e o cabo de energia.',
        'Em Atendimento',
        DATEADD(DAY, -2, GETDATE()),
        'usuario@email.com',
        'tecnico@email.com',
        NULL
    );
    PRINT '✅ Chamado criado: Computador não liga';
END

-- Chamado 3: Em Atendimento
IF NOT EXISTS (SELECT * FROM Chamados WHERE Assunto = 'Impressora não funciona')
BEGIN
    INSERT INTO Chamados (Assunto, Descricao, Status, DataAbertura, UsuarioCriadorEmail, TecnicoAtribuidoEmail, Rating)
    VALUES (
        'Impressora não funciona',
        'A impressora do financeiro parou de funcionar. Aparece mensagem de erro de driver.',
        'Em Atendimento',
        DATEADD(DAY, -1, GETDATE()),
        'usuario@email.com',
        'tecnico@email.com',
        NULL
    );
    PRINT '✅ Chamado criado: Impressora não funciona';
END

-- Chamado 4: Finalizado com Avaliação
IF NOT EXISTS (SELECT * FROM Chamados WHERE Assunto = 'Solicitação de novo recurso')
BEGIN
    INSERT INTO Chamados (Assunto, Descricao, Status, DataAbertura, UsuarioCriadorEmail, TecnicoAtribuidoEmail, Rating)
    VALUES (
        'Solicitação de novo recurso',
        'Gostaria de solicitar a implementação de relatórios mensais de chamados.',
        'Finalizado',
        DATEADD(DAY, -5, GETDATE()),
        'usuario@teste.com',
        'tecnico@email.com',
        5
    );
    PRINT '✅ Chamado criado: Solicitação de novo recurso';
END

-- Chamado 5: Resolvido pelo Chatbot
IF NOT EXISTS (SELECT * FROM Chamados WHERE Assunto = 'Como resetar minha senha?')
BEGIN
    INSERT INTO Chamados (Assunto, Descricao, Status, DataAbertura, UsuarioCriadorEmail, TecnicoAtribuidoEmail, Rating)
    VALUES (
        'Como resetar minha senha?',
        'Preciso redefinir minha senha mas não sei como fazer.',
        'Finalizado',
        DATEADD(HOUR, -3, GETDATE()),
        'jumento@gmail.com',
        NULL,
        4
    );
    PRINT '✅ Chamado criado: Como resetar minha senha?';
END

PRINT '';
PRINT '========================================';
PRINT 'CHAMADOS CRIADOS COM SUCESSO!';
PRINT '========================================';
GO

-- =====================================================
-- PASSO 8: INSERIR MENSAGENS DE CHAT DE EXEMPLO
-- =====================================================

PRINT '';
PRINT '========================================';
PRINT 'INSERINDO MENSAGENS DE CHAT...';
PRINT '========================================';

-- Buscar o protocolo do primeiro chamado criado
DECLARE @Protocolo1 INT = (SELECT TOP 1 Protocolo FROM Chamados WHERE Assunto = 'Computador não liga');
DECLARE @Protocolo2 INT = (SELECT TOP 1 Protocolo FROM Chamados WHERE Assunto = 'Impressora não funciona');

-- Mensagens para Chamado 1 (Computador não liga)
IF @Protocolo1 IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM ChatMessages WHERE TicketId = @Protocolo1)
    BEGIN
        INSERT INTO ChatMessages (TicketId, Timestamp, SenderEmail, SenderName, MessageText)
        VALUES 
            (@Protocolo1, DATEADD(DAY, -2, GETDATE()), 'usuario@email.com', 'Usuário da Silva', 'O computador parou de funcionar de repente.'),
            (@Protocolo1, DATEADD(DAY, -2, DATEADD(MINUTE, 5, GETDATE())), 'tecnico@email.com', 'Técnico da Silva', 'Entendido. Vou verificar no local.'),
            (@Protocolo1, DATEADD(DAY, -2, DATEADD(MINUTE, 30, GETDATE())), 'tecnico@email.com', 'Técnico da Silva', 'Parece ser problema na fonte. Vou trazer uma nova.');
        
        PRINT '✅ Mensagens criadas para o chamado: Computador não liga';
    END
END

-- Mensagens para Chamado 2 (Impressora não funciona)
IF @Protocolo2 IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM ChatMessages WHERE TicketId = @Protocolo2)
    BEGIN
        INSERT INTO ChatMessages (TicketId, Timestamp, SenderEmail, SenderName, MessageText)
        VALUES 
            (@Protocolo2, DATEADD(DAY, -1, GETDATE()), 'usuario@email.com', 'Usuário da Silva', 'A impressora não imprime há 2 dias.'),
            (@Protocolo2, DATEADD(DAY, -1, DATEADD(MINUTE, 10, GETDATE())), 'tecnico@email.com', 'Técnico da Silva', 'Já estou a caminho para verificar.');
        
        PRINT '✅ Mensagens criadas para o chamado: Impressora não funciona';
    END
END

PRINT '';
PRINT '========================================';
PRINT 'MENSAGENS DE CHAT CRIADAS!';
PRINT '========================================';
GO

-- =====================================================
-- PASSO 9: VERIFICAÇÃO FINAL
-- =====================================================

PRINT '';
PRINT '========================================';
PRINT '📊 RESUMO DO BANCO DE DADOS';
PRINT '========================================';
PRINT '';

-- Contar registros
DECLARE @TotalUsuarios INT = (SELECT COUNT(*) FROM Usuarios);
DECLARE @TotalChamados INT = (SELECT COUNT(*) FROM Chamados);
DECLARE @TotalMensagens INT = (SELECT COUNT(*) FROM ChatMessages);

PRINT '✅ Usuários criados: ' + CAST(@TotalUsuarios AS NVARCHAR(10));
PRINT '✅ Chamados criados: ' + CAST(@TotalChamados AS NVARCHAR(10));
PRINT '✅ Mensagens criadas: ' + CAST(@TotalMensagens AS NVARCHAR(10));

PRINT '';
PRINT '========================================';
PRINT '👥 USUÁRIOS DISPONÍVEIS PARA LOGIN';
PRINT '========================================';

-- Listar usuários com suas credenciais
SELECT 
    ROW_NUMBER() OVER (ORDER BY Role DESC, Nome) AS '#',
    Nome,
    Email,
    Senha AS [Senha (Texto Plano)],
    Role,
    FORMAT(DataCriacao, 'dd/MM/yyyy HH:mm', 'pt-BR') AS [Data Criação]
FROM Usuarios
ORDER BY 
    CASE Role 
        WHEN 'Gerente' THEN 1
        WHEN 'Tecnico' THEN 2
        WHEN 'Usuario' THEN 3
    END,
    Nome;

PRINT '';
PRINT '========================================';
PRINT '🎫 CHAMADOS CADASTRADOS';
PRINT '========================================';

SELECT 
    Protocolo,
    Assunto,
    Status,
    UsuarioCriadorEmail AS [Criado Por],
    TecnicoAtribuidoEmail AS [Técnico],
    FORMAT(DataAbertura, 'dd/MM/yyyy HH:mm', 'pt-BR') AS [Data],
    ISNULL(CAST(Rating AS NVARCHAR), 'Não avaliado') AS [Avaliação]
FROM Chamados
ORDER BY DataAbertura DESC;

PRINT '';
PRINT '========================================';
PRINT '✅ BANCO DE DADOS CRIADO COM SUCESSO!';
PRINT '========================================';
PRINT '';
PRINT '🔐 CREDENCIAIS DE ACESSO RÁPIDO:';
PRINT '   ┌─────────────────────────────────────────────────┐';
PRINT '   │ GERENTES:                                        │';
PRINT '   │  • gerente@sistema.com  / senha123              │';
PRINT '   │  • gerente@email.com    / 123456                │';
PRINT '   ├─────────────────────────────────────────────────┤';
PRINT '   │ TÉCNICOS:                                        │';
PRINT '   │  • admin@gestaochamados.com / admin123          │';
PRINT '   │  • tecnico@email.com    / 123456                │';
PRINT '   │  • tecnico@teste.com    / Tecnico@123           │';
PRINT '   ├─────────────────────────────────────────────────┤';
PRINT '   │ USUÁRIOS:                                        │';
PRINT '   │  • usuario@email.com    / 123456                │';
PRINT '   │  • usuario@teste.com    / user123               │';
PRINT '   │  • jumento@gmail.com    / senha123              │';
PRINT '   └─────────────────────────────────────────────────┘';
PRINT '';
PRINT '📝 PRÓXIMOS PASSOS:';
PRINT '   1. Configure a connection string no appsettings.json';
PRINT '   2. Execute: dotnet restore';
PRINT '   3. Execute: dotnet run';
PRINT '   4. Acesse: http://localhost:5013';
PRINT '';
PRINT '⚠️  IMPORTANTE: Senhas em TEXTO PLANO (migrar para BCrypt depois)';
PRINT '   • Para migrar: Execute Migrar-Senhas-BCrypt.ps1';
PRINT '   • Ou use: POST /api/auth/migrate-passwords';
PRINT '';
PRINT '========================================';
PRINT '🎉 SETUP COMPLETO!';
PRINT '========================================';
GO
