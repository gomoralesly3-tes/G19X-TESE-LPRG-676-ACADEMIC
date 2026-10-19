-- ============================================================
-- respaldo_manual.sql  -  Respaldo completo AHORA (cualquier edicion)
-- Util en SQL Server Express (sin Agent) o antes de un cambio importante.
-- Guarda el archivo en la carpeta de respaldos del servidor, subcarpeta MonitoreoOC.
-- ============================================================
USE master;
GO

DECLARE @base NVARCHAR(260);
EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE',
     N'Software\Microsoft\MSSQLServer\MSSQLServer', N'BackupDirectory', @base OUTPUT;

IF @base IS NULL
    THROW 50100, N'No se encontro la carpeta de respaldos del servidor.', 1;

DECLARE @carpeta NVARCHAR(300) = @base + N'\MonitoreoOC';
EXEC master.dbo.xp_create_subdir @carpeta;

DECLARE @archivo NVARCHAR(400) = @carpeta + N'\MonitoreoOrdenesCompra_'
    + CONVERT(CHAR(8), GETDATE(), 112) + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), ':', '') + N'.bak';

BACKUP DATABASE [MonitoreoOrdenesCompra] TO DISK = @archivo
WITH INIT, CHECKSUM, NAME = N'MonitoreoOrdenesCompra - manual';

RESTORE VERIFYONLY FROM DISK = @archivo WITH CHECKSUM;

PRINT N'Respaldo creado y verificado: ' + @archivo;
GO
