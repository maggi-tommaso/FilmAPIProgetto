$ErrorActionPreference = "Stop"

function Wait-TcpPort {
  param(
    [Parameter(Mandatory=$true)][string]$HostName,
    [Parameter(Mandatory=$true)][int]$Port,
    [int]$TimeoutSeconds = 60
  )

  $sw = [Diagnostics.Stopwatch]::StartNew()
  while ($sw.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
    try {
      $client = New-Object System.Net.Sockets.TcpClient
      $iar = $client.BeginConnect($HostName, $Port, $null, $null)
      if ($iar.AsyncWaitHandle.WaitOne(500)) {
        $client.EndConnect($iar)
        $client.Close()
        return $true
      }
      $client.Close()
    } catch {
      # ignore
    }
    Start-Sleep -Milliseconds 500
  }
  return $false
}

Write-Host "Avvio MariaDB (Docker Compose)..."
docker compose up -d db | Out-Null

Write-Host "Attendo MariaDB su localhost:3306..."
if (-not (Wait-TcpPort -HostName "127.0.0.1" -Port 3306 -TimeoutSeconds 90)) {
  throw "MariaDB non risponde su localhost:3306 (controlla Docker Desktop)."
}

Write-Host "Avvio backend (FilmAPI) e frontend (SalaLuce.Web)..."

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Start-Process -FilePath "powershell" -ArgumentList "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", "cd `"$root`"; dotnet run --project `"FilmAPI.csproj`"" | Out-Null
Start-Process -FilePath "powershell" -ArgumentList "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", "cd `"$root`"; dotnet run --project `"SalaLuce.Web\SalaLuce.Web.csproj`"" | Out-Null

Write-Host ""
Write-Host "Frontend:  http://localhost:5076"
Write-Host "Backend:   http://localhost:5072 (Swagger: /swagger)"
Write-Host ""
Write-Host "Per fermare il DB: docker compose down"

