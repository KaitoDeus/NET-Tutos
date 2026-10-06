# Script khoi dong nhanh ca Backend va App Mobile .NET MAUI
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   KHOI DONG HE THONG .NET TUTOS (WEB API + MOBILE APP)   " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan

$adbPath = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
$emulatorPath = "$env:LOCALAPPDATA\Android\Sdk\emulator\emulator.exe"
$apkPath = "d:\.ASPNET-Tutos\NET-Tutos.Mobile\bin\Debug\net10.0-android\com.nettutos.mobile-Signed.apk"

# 1. Khoi dong Backend Web API (neu chua chay)
$portOpen = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if (-not $portOpen) {
    Write-Host "`n[1/3] Dang khoi dong Web API tren cong 5000 o cua so moi..." -ForegroundColor Green
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project d:\.ASPNET-Tutos\NET-Tutos.WebApp\NET-Tutos.WebApp.csproj"
    Start-Sleep -Seconds 3
} else {
    Write-Host "`n[1/3] Web API da dang chay tren cong 5000." -ForegroundColor Green
}

# 2. Kiem tra thiet bi Android
Write-Host "[2/3] Kiem tra may ao Android Pixel_9a..." -ForegroundColor Green
$devices = & $adbPath devices
$hasDevice = $devices | Where-Object { $_ -match "\bdevice\b" -and $_ -notmatch "List of devices" }

if (-not $hasDevice) {
    Write-Host "Dang khoi dong may ao Pixel_9a... Vui long doi may ao khoi dong xong..." -ForegroundColor Yellow
    Start-Process $emulatorPath -ArgumentList "-avd", "Pixel_9a"
    Write-Host "Dang doi may ao san sang (adb wait-for-device)..." -ForegroundColor Yellow
    & $adbPath wait-for-device
    Start-Sleep -Seconds 8
} else {
    Write-Host "Da ket noi voi thiet bi Android!" -ForegroundColor Green
}

# 3. Cai dat va khoi chay App
Write-Host "[3/3] Dang cai dat va mo app .NET Tutos tren may ao..." -ForegroundColor Green
& $adbPath install -r $apkPath
& $adbPath shell monkey -p com.nettutos.mobile -c android.intent.category.LAUNCHER 1

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "   APP DA DUOC MO THANH CONG TREN MAN HINH ANDROID!       " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
