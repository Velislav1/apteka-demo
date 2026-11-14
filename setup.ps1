# setup.ps1 — инсталира всичко нужно за демото на аптечната програма (Windows 10/11)
# Пуска се ВЕДНЪЖ на лаптопа, в PowerShell:  powershell -ExecutionPolicy Bypass -File .\setup.ps1
# Ползва winget (вграден в Windows). Може да поиска администраторски права (UAC прозорец).

$ErrorActionPreference = 'Continue'

function Test-Cmd($name) { [bool](Get-Command $name -ErrorAction SilentlyContinue) }

function Install-Pkg($id, $label) {
    Write-Host "`n=== $label ($id) ===" -ForegroundColor Cyan
    $installed = winget list --id $id --exact --accept-source-agreements 2>$null | Select-String $id
    if ($installed) {
        Write-Host "Вече е инсталиран." -ForegroundColor Green
    } else {
        winget install --id $id --exact --silent --accept-package-agreements --accept-source-agreements
    }
}

if (-not (Test-Cmd 'winget')) {
    Write-Host "winget липсва. Инсталирайте 'App Installer' от Microsoft Store и пуснете скрипта отново." -ForegroundColor Red
    exit 1
}

Install-Pkg 'Microsoft.DotNet.SDK.10'  '.NET 10 SDK'
Install-Pkg 'Git.Git'                  'Git'
Install-Pkg 'Docker.DockerDesktop'     'Docker Desktop'

# Обновяване на PATH в текущия прозорец, за да се виждат новите програми
$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')

Write-Host "`n=== Инструменти за .NET ===" -ForegroundColor Cyan
if (Test-Cmd 'dotnet') {
    dotnet tool install --global dotnet-ef 2>$null
    if ($LASTEXITCODE -ne 0) { dotnet tool update --global dotnet-ef }
}

Write-Host "`n=== Разширения за VS Code ===" -ForegroundColor Cyan
if (Test-Cmd 'code') {
    code --install-extension ms-dotnettools.csdevkit --force
    code --install-extension ms-azuretools.vscode-docker --force
} else {
    Write-Host "VS Code командата 'code' не е намерена — инсталирайте 'C# Dev Kit' ръчно от Extensions." -ForegroundColor Yellow
}

Write-Host "`n=== Проверка ===" -ForegroundColor Cyan
foreach ($c in 'dotnet','git','docker') {
    if (Test-Cmd $c) { Write-Host "$c : OK" -ForegroundColor Green } else { Write-Host "$c : ЛИПСВА (рестартирайте компютъра и пуснете скрипта пак)" -ForegroundColor Red }
}
if (Test-Cmd 'dotnet') { dotnet --list-sdks }

Write-Host @"

ГОТОВО. Следващи стъпки:
 1. Ако Docker Desktop е инсталиран сега — РЕСТАРТИРАЙТЕ компютъра, после го отворете веднъж
    и изчакайте да пише 'Engine running' (може да поиска да включи WSL 2 — съгласете се).
 2. Отворете тази папка във VS Code и стартирайте Claude Code.
 3. Пратете 'Промпт 0' от PROMPTS.md.
"@ -ForegroundColor Cyan
