-- ============================================================
-- crear_job_respaldo.sql  -  Respaldo completo diario (SQL Server Agent)
--
-- Cada dia a las 02:00:
--   1. Respalda MonitoreoOrdenesCompra en un archivo .bak con la fecha en el nombre.
--   2. Comprueba que el archivo sea legible (RESTORE VERIFYONLY).
--   3. Borra los respaldos de mas de @DiasConservar dias.
--
-- Por omision usa la carpeta de respaldos del servidor SQL (el servicio ya tiene
-- permiso de escritura) y crea dentro la subcarpeta MonitoreoOC. Para usar otra
-- carpeta, escribela en @CarpetaBase (el servicio de SQL Server debe poder
-- escribir en ella).
--
-- SOLO sirve con SQL Server Agent (Standard, Enterprise o Developer). En Express
-- no existe: usa respaldo_manual.sql o el Programador de tareas con sqlcmd.
-- Se puede ejecutar varias veces: recrea el trabajo.
--
-- IMPORTANTE: los respaldos quedan en LA MISMA computadora. Si el disco falla,
-- se pierden junto con la base. Copia la carpeta a otro disco o a la nube.
-- ============================================================
USE msdb;
GO

DECLARE @job           NVARCHAR(128) = N'MonitoreoOC - Respaldo diario';
DECLARE @CarpetaBase   NVARCHAR(260) = NULL;   -- NULL = carpeta de respaldos del servidor. Ejemplo: N'D:\Respaldos'
DECLARE @DiasConservar INT           = 14;

IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = @job)
    EXEC msdb.dbo.sp_delete_job @job_name = @job;

DECLARE @cmd NVARCHAR(MAX) = N'
SET NOCOUNT ON;

DECLARE @base NVARCHAR(260) = ' + CASE WHEN @CarpetaBase IS NULL THEN N'NULL' ELSE QUOTENAME(@CarpetaBase, '''') END + N';

IF @base IS NULL
    EXEC master.dbo.xp_instance_regread N''HKEY_LOCAL_MACHINE'',
         N''Software\Microsoft\MSSQLServer\MSSQLServer'', N''BackupDirectory'', @base OUTPUT;

IF @base IS NULL
    THROW 50100, N''No se encontro la carpeta de respaldos del servidor.'', 1;

-- Subcarpeta propia: asi la limpieza solo toca los respaldos de este sistema
DECLARE @carpeta NVARCHAR(300) = @base + N''\MonitoreoOC'';
EXEC master.dbo.xp_create_subdir @carpeta;

DECLARE @archivo NVARCHAR(400) = @carpeta + N''\MonitoreoOrdenesCompra_'' + CONVERT(CHAR(8), GETDATE(), 112) + N''.bak'';

BACKUP DATABASE [MonitoreoOrdenesCompra] TO DISK = @archivo
WITH INIT, CHECKSUM, NAME = N''MonitoreoOrdenesCompra - completo'';

RESTORE VERIFYONLY FROM DISK = @archivo WITH CHECKSUM;

-- Limpieza: borra los .bak de la subcarpeta con mas de N dias
DECLARE @limite DATETIME = DATEADD(DAY, -' + CAST(@DiasConservar AS NVARCHAR(10)) + N', GETDATE());
EXEC master.dbo.xp_delete_file 0, @carpeta, N''bak'', @limite;
';

EXEC msdb.dbo.sp_add_job
     @job_name         = @job,
     @enabled          = 1,
     @owner_login_name = N'sa',     -- evita el error 15404 con cuentas de Windows
     @description      = N'Respaldo completo diario de MonitoreoOrdenesCompra con verificacion y limpieza.';

EXEC msdb.dbo.sp_add_jobstep
     @job_name      = @job,
     @step_name     = N'Respaldar, verificar y limpiar',
     @subsystem     = N'TSQL',
     @database_name = N'master',
     @command       = @cmd;

EXEC msdb.dbo.sp_add_jobschedule
     @job_name          = @job,
     @name              = N'Diario 02:00',
     @freq_type         = 4,        -- diario
     @freq_interval     = 1,        -- todos los dias
     @active_start_time = 20000;    -- 02:00:00

EXEC msdb.dbo.sp_add_jobserver @job_name = @job, @server_name = N'(local)';
GO

-- ============================================================
-- Probar ahora mismo (opcional)
-- ============================================================
-- EXEC msdb.dbo.sp_start_job @job_name = N'MonitoreoOC - Respaldo diario';
--
-- Ver el resultado: clic derecho en el trabajo > "View History".
--
-- Ver donde quedaron los archivos:
-- SELECT TOP 5 physical_device_name, backup_finish_date
-- FROM msdb.dbo.backupmediafamily f
-- JOIN msdb.dbo.backupset s ON s.media_set_id = f.media_set_id
-- WHERE s.database_name = N'MonitoreoOrdenesCompra'
-- ORDER BY s.backup_finish_date DESC;
--
-- MODELO DE RECUPERACION: si esta en FULL y no haces respaldos del registro de
-- transacciones, el archivo .ldf crece sin parar. Para este sistema, con un
-- respaldo diario, suele bastar el modo SIMPLE (se pueden perder hasta 24 h de
-- datos en un desastre). Revisa el actual con:
--   SELECT name, recovery_model_desc FROM sys.databases WHERE name = N'MonitoreoOrdenesCompra';
-- y, si decides cambiarlo:
--   ALTER DATABASE [MonitoreoOrdenesCompra] SET RECOVERY SIMPLE;
--
-- ============================================================
-- RESTAURAR un respaldo (en SSMS: clic derecho en Databases > Restore Database...)
-- o con T-SQL (cambia la ruta al archivo real):
-- ============================================================
-- USE master;
-- ALTER DATABASE [MonitoreoOrdenesCompra] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
-- RESTORE DATABASE [MonitoreoOrdenesCompra]
--     FROM DISK = N'C:\...\MonitoreoOC\MonitoreoOrdenesCompra_20261016.bak' WITH REPLACE;
-- ALTER DATABASE [MonitoreoOrdenesCompra] SET MULTI_USER;
