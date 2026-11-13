<#
.SYNOPSIS
    Khởi động ứng dụng .NET MAUI 10 Mobile (.NET-Tutos) trên Android Emulator.
.DESCRIPTION
    Tự động kiểm tra JDK 17, khởi động Backend Web BFF (cổng 5262), 
    mở Android Emulator (NetTutos_API34), và build/deploy app lên thiết bị.
#>

param (
    [switch]$SkipBackend = $false,
    [switch]$SkipBuild = $false,
    [string]$AvdName = "NetTutos_API34",
    [int]$WebPort = 5262
)

$ErrorActionPreference = "Continue"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   KHOI DONG .NET MAUI 10 MOBILE - NET-TUTOS (PHASE 27)  " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Thiet lap moi truong Android & JDK
$androidSdk = "$env:LOCALAPPDATA\Android\Sdk"
$jdkPath = "C:\Program Files\Microsoft\jdk-17.0.21.21-hotspot"

if (-not (Test-Path $jdkPath)) {
    # Thu tim cac thu muc JDK khac
    $altJdk = Get-ChildItem "C:\Program Files\Microsoft" -Filter "jdk-17*" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
    if ($altJdk) {
        $jdkPath = $altJdk
    } else {
        Write-Error "Khong tim thay OpenJDK 17 tai $jdkPath. Vui long cai dat Microsoft OpenJDK 17."
    }
}

$env:JAVA_HOME = $jdkPath
$env:ANDROID_HOME = $androidSdk
$env:ANDROID_SDK_ROOT = $androidSdk
$env:PATH = "$jdkPath\bin;$androidSdk\platform-tools;$androidSdk\emulator;$env:PATH"
$adb = "$androidSdk\platform-tools\adb.exe"
$emulator = "$androidSdk\emulator\emulator.exe"

Write-Host "[1/4] Moi truong:" -ForegroundColor Green
Write-Host "  - JDK: $jdkPath"
Write-Host "  - Android SDK: $androidSdk"

# 2. Khoi dong Backend Web BFF neu chua chay
if (-not $SkipBackend) {
    Write-Host "`n[2/4] Kiem tra Backend Web BFF (cong $WebPort)..." -ForegroundColor Green
    $backendRunning = $false
    try {
        $res = Invoke-WebRequest -Uri "http://127.0.0.1:$WebPort/api/mobile/atus/status" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($res.StatusCode -eq 200) { $backendRunning = $true }
    } catch {
        $backendRunning = $false
    }

    if ($backendRunning) {
        Write-Host "  -> Backend Web BFF dang chay tren cong $WebPort (San sang)." -ForegroundColor Yellow
    } else {
        Write-Host "  -> Dang khoi dong Web BFF tren cong $WebPort o cua so rieng..." -ForegroundColor Yellow
        $projectPath = Join-Path $PSScriptRoot "..\src\Presentation\Web\web.csproj"
        $resolvedProjectPath = (Resolve-Path $projectPath).Path
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project `"$resolvedProjectPath`" --no-launch-profile --urls `"http://127.0.0.1:$WebPort`"" -WindowStyle Minimized
        
        Write-Host "  -> Cho Web BFF san sang..." -NoNewline
        $attempts = 0
        while ($attempts -lt 20) {
            Start-Sleep -Seconds 1
            $attempts++
            try {
                $check = Invoke-WebRequest -Uri "http://127.0.0.1:$WebPort/api/mobile/atus/status" -UseBasicParsing -TimeoutSec 1 -ErrorAction SilentlyContinue
                if ($check.StatusCode -eq 200) {
                    Write-Host " OK!" -ForegroundColor Green
                    break
                }
            } catch {
                Write-Host "." -NoNewline
            }
        }
    }
}

# 3. Kiem tra va khoi dong Android Emulator
Write-Host "`n[3/4] Kiem tra Android Emulator..." -ForegroundColor Green
$attached = & $adb devices | Select-String "emulator-\d+\s+(device|offline)"

if (-not $attached) {
    Write-Host "  -> Chua co may ao hoat dong. Dang khoi dong $AvdName..." -ForegroundColor Yellow
    Start-Process $emulator -ArgumentList "-avd", $AvdName, "-netdelay", "none", "-netspeed", "full" -WorkingDirectory "$androidSdk\emulator" -WindowStyle Normal
} else {
    Write-Host "  -> May ao Android da duoc bat..." -ForegroundColor Yellow
}

Write-Host "  -> Dang cho Android khoi dong hoan tat (boot_completed)..." -NoNewline
$booted = $false
$timeoutSeconds = 90
$elapsed = 0
while (-not $booted -and $elapsed -lt $timeoutSeconds) {
    Start-Sleep -Seconds 3
    $elapsed += 3
    Write-Host "." -NoNewline
    $bootProp = (& $adb shell getprop sys.boot_completed 2>&1 | Out-String).Trim()
    if ($bootProp -eq "1") {
        $booted = $true
        Write-Host " Hoan tat!" -ForegroundColor Green
    }
}
if (-not $booted) {
    Write-Warning "May ao chua phan hoi boot_completed=1, tiep tuc tien hanh..."
}

# 4. Build va Chay app Mobile
if (-not $SkipBuild) {
    Write-Host "`n[4/4] Bien dich va mo ung dung tren may ao..." -ForegroundColor Green
    $mobileProject = Join-Path $PSScriptRoot "..\src\Presentation\Mobile\NET_Tutos.Mobile.csproj"
    $resolvedMobile = (Resolve-Path $mobileProject).Path
    
    $targetDevice = & $adb devices | Select-String "emulator-\d+\s+device" | ForEach-Object { ($_ -split "\s+")[0] } | Select-Object -First 1
    if ($targetDevice) {
        Write-Host "  -> Deploying toi device: $targetDevice" -ForegroundColor Cyan
        dotnet build $resolvedMobile -f net10.0-android -t:Run -p:JavaSdkDirectory="$jdkPath" -p:AdbTarget="-s $targetDevice" --no-restore --nologo
    } else {
        dotnet build $resolvedMobile -f net10.0-android -t:Run -p:JavaSdkDirectory="$jdkPath" --no-restore --nologo
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n>>> KHOI DONG THANH CONG! App da duoc mo tren Android Emulator. <<<" -ForegroundColor Green
        Write-Host "Luu y ket noi:"
        Write-Host "  - May ao Android ket noi toi Backend Windows qua: http://10.0.2.2:$WebPort/api/mobile"
        Write-Host "  - Ban co the vao muc 'Cai dat' tren app de kiem tra hoac cap nhat Server URL neu can."
    } else {
        Write-Host "`nBuild/Run that bai. Ma loi: $LASTEXITCODE" -ForegroundColor Red
    }
}
