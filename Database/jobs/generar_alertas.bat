@echo off
REM ============================================================
REM generar_alertas.bat
REM Ejecuta dbo.sp_GenerarAlertas. Para SQL Server Express (sin Agent):
REM programalo con el Programador de tareas de Windows (ver README.md).
REM Cambia SERVIDOR si tu servidor no es DSKTPA. Si usas la instancia
REM Express por defecto, puede ser:  DSKTPA\SQLEXPRESS
REM ============================================================
set SERVIDOR=DSKTPA
sqlcmd -S %SERVIDOR% -E -d MonitoreoOrdenesCompra -b -Q "EXEC dbo.sp_GenerarAlertas;" >> "%~dp0generar_alertas.log" 2>&1
if errorlevel 1 echo %date% %time% ERROR al generar alertas >> "%~dp0generar_alertas.log"
