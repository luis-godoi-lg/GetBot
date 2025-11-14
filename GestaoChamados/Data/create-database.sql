-- Script de criação do banco de dados GestaoChamadosDB
-- SQL Server

-- Criar banco de dados
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'GestaoChamadosDB')
BEGIN
    CREATE DATABASE GestaoChamadosDB;
END
GO

USE GestaoChamadosDB;
GO

-- Tabela de Usuários
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Usuarios')
BEGIN
    CREATE TABLE Usuarios (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(200) NOT NULL,
        Email NVARCHAR(200) NOT NULL UNIQUE,
        Senha NVARCHAR(500) NOT NULL,
        Role NVARCHAR(50) NOT NULL CHECK (Role IN ('Usuario', 'Tecnico'))
    );
END
GO

-- Tabela de Chamados
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
        Rating INT NULL CHECK (Rating >= 1 AND Rating <= 5),
        FOREIGN KEY (UsuarioCriadorEmail) REFERENCES Usuarios(Email)
    );
END
GO

-- Tabela de Mensagens de Chat
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatMessages')
BEGIN
    CREATE TABLE ChatMessages (
        TicketId INT NOT NULL,
        Timestamp DATETIME2 NOT NULL,
        SenderEmail NVARCHAR(200) NOT NULL,
        SenderName NVARCHAR(200) NOT NULL,
        MessageText NVARCHAR(2000) NOT NULL,
        PRIMARY KEY (TicketId, Timestamp),
        FOREIGN KEY (TicketId) REFERENCES Chamados(Protocolo) ON DELETE CASCADE
    );
END
GO

-- Índices para melhor performance
CREATE INDEX IX_Chamados_Status ON Chamados(Status);
CREATE INDEX IX_Chamados_UsuarioCriadorEmail ON Chamados(UsuarioCriadorEmail);
CREATE INDEX IX_Chamados_TecnicoAtribuidoEmail ON Chamados(TecnicoAtribuidoEmail);
CREATE INDEX IX_Chamados_DataAbertura ON Chamados(DataAbertura DESC);
CREATE INDEX IX_ChatMessages_TicketId ON ChatMessages(TicketId);
GO

-- Dados iniciais de teste
-- Usuário Admin/Técnico (senha: admin123)
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'admin@gestaochamados.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role)
    VALUES ('Administrador', 'admin@gestaochamados.com', 'admin123', 'Tecnico');
END
GO

-- Usuário comum (senha: user123)
IF NOT EXISTS (SELECT * FROM Usuarios WHERE Email = 'usuario@teste.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, Senha, Role)
    VALUES ('Usuário Teste', 'usuario@teste.com', 'user123', 'Usuario');
END
GO

-- Chamado de exemplo
IF NOT EXISTS (SELECT * FROM Chamados WHERE Protocolo = 1)
BEGIN
    INSERT INTO Chamados (Assunto, Descricao, Status, DataAbertura, UsuarioCriadorEmail)
    VALUES 
    ('Problema com acesso ao sistema', 'Não consigo fazer login no sistema desde ontem', 'Aberto', GETDATE(), 'usuario@teste.com'),
    ('Solicitação de novo recurso', 'Gostaria de solicitar a implementação de relatórios mensais', 'Em Atendimento', DATEADD(HOUR, -2, GETDATE()), 'usuario@teste.com');
END
GO

PRINT 'Banco de dados GestaoChamadosDB criado com sucesso!';
PRINT 'Usuário Admin: admin@gestaochamados.com (senha: admin123)';
PRINT 'Usuário Teste: usuario@teste.com (senha: user123)';
GO
