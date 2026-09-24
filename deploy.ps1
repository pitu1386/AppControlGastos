param (
    [string]$RepoUrl = "https://github.com/pitu1386/AppControlGastos.git",
    [string]$RepoName = "AppControlGastos"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Despliegue PWA en GitHub Pages            " -ForegroundColor Cyan
Write-Host " Repositorio: $RepoName                   " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Limpieza de carpetas previas
if (Test-Path .\publish_output) {
    Write-Host "Limpiando directorio publish_output anterior..." -ForegroundColor Gray
    Remove-Item .\publish_output -Recurse -Force
}

# 2. Compilación de producción .NET
Write-Host "Compilando version de produccion en Release..." -ForegroundColor Cyan
dotnet publish .\AppControlGastos.csproj -c Release -o .\publish_output

if ($LASTEXITCODE -ne 0) {
    Write-Error "Fallo la compilacion de produccion."
    exit $LASTEXITCODE
}

# 3. Configuración para GitHub Pages
Write-Host "Configurando GitHub Pages (.nojekyll, 404.html, base href)..." -ForegroundColor Cyan

# Archivo .nojekyll para que GitHub Pages no ignore carpetas con guion bajo (_framework)
New-Item -ItemType File -Force -Path ".\publish_output\wwwroot\.nojekyll" | Out-Null

# Copiar archivos sin huella hash si aplica para máxima compatibilidad
$wasmJs = Get-ChildItem -Path ".\publish_output\wwwroot\_framework\blazor.webassembly*.js" | Select-Object -First 1
if ($wasmJs -and ($wasmJs.Name -ne "blazor.webassembly.js")) {
    Copy-Item $wasmJs.FullName -Destination ".\publish_output\wwwroot\_framework\blazor.webassembly.js" -Force
}

$dotnetJs = Get-ChildItem -Path ".\publish_output\wwwroot\_framework\dotnet*.js" | Where-Object { $_.Name -notmatch "runtime|native" } | Select-Object -First 1
if ($dotnetJs) {
    Copy-Item $dotnetJs.FullName -Destination ".\publish_output\wwwroot\_framework\dotnet.js" -Force
}

# Ajustar base href para subcarpeta en GitHub Pages: /<RepoName>/
$indexPath = ".\publish_output\wwwroot\index.html"
$content = [System.IO.File]::ReadAllText($indexPath, [System.Text.Encoding]::UTF8)
$content = $content.Replace('<base href="/" />', "<base href=""/$RepoName/"" />")
if ($wasmJs) {
    $content = $content.Replace('_framework/blazor.webassembly.js', "_framework/$($wasmJs.Name)")
}
[System.IO.File]::WriteAllText($indexPath, $content, [System.Text.Encoding]::UTF8)

# Recalcular hash de index.html para service-worker-assets.js (Evitar fallo de SRI)
$swAssetsPath = ".\publish_output\wwwroot\service-worker-assets.js"
if (Test-Path $swAssetsPath) {
    Write-Host "Actualizando hash SHA-256 de index.html en service-worker-assets.js..." -ForegroundColor Gray
    $bytes = [System.IO.File]::ReadAllBytes($indexPath)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $hash = [Convert]::ToBase64String($sha256.ComputeHash($bytes))
    $newHash = "sha256-$hash"
    
    $swContent = [System.IO.File]::ReadAllText($swAssetsPath, [System.Text.Encoding]::UTF8)
    $swContent = [System.Text.RegularExpressions.Regex]::Replace(
        $swContent,
        '("hash":\s*"sha256-[^"]+",\s*"url":\s*"index\.html")',
        """hash"": ""$newHash"",`n        ""url"": ""index.html"""
    )
    [System.IO.File]::WriteAllText($swAssetsPath, $swContent, [System.Text.Encoding]::UTF8)
}

# Copiar index.html como 404.html para soporte de enrutamiento SPA en GitHub Pages
Copy-Item -Path $indexPath -Destination ".\publish_output\wwwroot\404.html" -Force

# 4. Despliegue en la rama gh-pages
Write-Host "Desplegando en la rama gh-pages de GitHub..." -ForegroundColor Cyan
$tempDeploy = Join-Path $env:TEMP "gastos_ghpages_deploy"
if (Test-Path $tempDeploy) { Remove-Item $tempDeploy -Recurse -Force }
Copy-Item ".\publish_output\wwwroot" -Destination $tempDeploy -Recurse -Force

Push-Location $tempDeploy
try {
    git init -b gh-pages
    git add -A
    git commit -m "Deploy PWA produccion a GitHub Pages"
    git remote add origin $RepoUrl
    Write-Host "Subiendo a origin gh-pages (forzado)..." -ForegroundColor Cyan
    git push -f origin gh-pages
    Write-Host "Despliegue completado con exito!" -ForegroundColor Green
    Write-Host "URL publica: https://pitu1386.github.io/$RepoName/" -ForegroundColor Yellow
}
catch {
    Write-Warning "No se pudo realizar el push automatico (comprueba que el repositorio remoto exista en GitHub): $_"
}
finally {
    Pop-Location
    if (Test-Path $tempDeploy) { Remove-Item $tempDeploy -Recurse -Force }
}
