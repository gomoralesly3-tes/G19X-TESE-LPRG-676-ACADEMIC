-- ============================================================
-- crear_job_alertas.sql  -  Trabajo de SQL Server Agent
-- Ejecuta dbo.sp_GenerarAlertas cada hora.
-- (El procedimiento no duplica alertas: genera como maximo una por orden
--  y tipo cada dia, asi que repetirlo cada hora es seguro.)
--
-- SOLO sirve si SQL Server Agent existe en tu servidor (ediciones
-- Standard, Enterprise o Developer). En Express NO existe: usa en su lugar
-- generar_alertas.bat con el Programador de tareas de Windows.
-- Se puede ejecutar varias veces: recrea el trabajo.
-- ============================================================
USE msdb;
GO

DECLARE @job NVARCHAR(128) = N'MonitoreoOC - Generar alertas';

IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = @job)
    EXEC msdb.dbo.sp_delete_job @job_name = @job;

-- El propietario es "sa" a proposito: si el duenio es una cuenta de Windows
-- (por ejemplo EQUIPO\Usuario), el trabajo puede fallar con el error 15404
-- ("Unable to determine if the owner ... has server access").
EXEC msdb.dbo.sp_add_job
     @job_name         = @job,
     @enabled          = 1,
     @owner_login_name = N'sa',
     @description = N'Genera las alertas automaticas de MonitoreoOrdenesCompra (sp_GenerarAlertas).';

EXEC msdb.dbo.sp_add_jobstep
     @job_name      = @job,
     @step_name     = N'Ejecutar sp_GenerarAlertas',
     @subsystem     = N'TSQL',
     @database_name = N'MonitoreoOrdenesCompra',
     @command       = N'EXEC dbo.sp_GenerarAlertas;';

EXEC msdb.dbo.sp_add_jobschedule
     @job_name             = @job,
     @name                 = N'Cada hora',
     @freq_type            = 4,   -- diario
     @freq_interval        = 1,   -- todos los dias
     @freq_subday_type     = 8,   -- unidad: horas
     @freq_subday_interval = 1;   -- cada 1 hora

EXEC msdb.dbo.sp_add_jobserver @job_name = @job, @server_name = N'(local)';
GO

-- Probar ahora mismo (opcional):
-- EXEC msdb.dbo.sp_start_job @job_name = N'MonitoreoOC - Generar alertas';
-- Ver resultado: clic derecho en el trabajo > "View History".
