-- Script para limpar dados de chamados e mensagens
-- Mantém os usuários cadastrados

USE GestaoChamadosDB;
GO

-- Limpar mensagens de chat
DELETE FROM ChatMessages;
DBCC CHECKIDENT ('ChatMessages', RESEED, 0);
GO

-- Limpar chamados
DELETE FROM Chamados;
DBCC CHECKIDENT ('Chamados', RESEED, 0);
GO

PRINT 'Dados de chamados e mensagens foram limpos com sucesso!';
PRINT 'Os usuários foram mantidos.';
GO
