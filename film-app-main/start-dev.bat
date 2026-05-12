@echo off
title CineBase - Full Stack (Dev Mode)
echo ========================================
echo   CineBase - Full Stack Development
echo ========================================
echo.

echo [1/3] Verifica database (MariaDB su localhost:3306)...
docker compose -f "%~dp0docker-compose.yml" exec -T db mariadb -u root -proot -e "SELECT 1" 2>nul >nul
IF %ERRORLEVEL% EQU 0 (
    echo Database gia' in esecuzione.
    goto :start_services
)

echo Database non attivo, avvio container...
docker compose -f "%~dp0docker-compose.yml" up -d db
IF %ERRORLEVEL% NEQ 0 (
    echo ERRORE: Impossibile avviare il database. Docker Desktop e' in esecuzione?
    pause
    exit /b 1
)

echo In attesa che il database sia pronto...
:wait_db
docker compose -f "%~dp0docker-compose.yml" exec -T db healthcheck.sh --connect --innodb_initialized 2>nul
IF %ERRORLEVEL% NEQ 0 (
    timeout /t 2 /nobreak >nul
    goto wait_db
)

:start_services
echo.
echo [2/3] Arresto eventuali processi backend/frontend esistenti...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":5000 .*LISTENING"') do taskkill /F /PID %%a 2>nul
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":5001 .*LISTENING"') do taskkill /F /PID %%a 2>nul
timeout /t 2 /nobreak >nul

echo [3/3] Avvio backend (http://localhost:5000)...
start "CineBase Backend (5000)" cmd /c "dotnet run --project backend\FilmAPI\FilmAPI.csproj"

echo [4/4] Avvio frontend (http://localhost:5001)...
start "CineBase Frontend (5001)" cmd /c "dotnet run --project frontend\CineBase.Web\CineBase.Web.csproj"

echo.
echo ========================================
echo   Servizi in avvio:
echo     Database:  localhost:3306
echo     Backend:   http://localhost:5000
echo     Frontend:  http://localhost:5001
echo     Swagger:   http://localhost:5000/swagger
echo     Login:     admin@cinebase.it / Admin123!
echo ========================================
echo.
pause
