<#
.SYNOPSIS
    Tự động tạo nhánh (Branch), Commit, Push lên GitHub và kích hoạt tạo Pull Request tự động cho dự án NET-Tutos.
.DESCRIPTION
    1. Nếu đang ở nhánh 'main', script tự động chuyển sang nhánh mới (ví dụ: feat/auto-pr-20261103-2010).
    2. Tự động git add -A và git commit nếu còn thay đổi chưa commit.
    3. Tự động git push -u origin <branchName>.
    4. Ngay khi push lên GitHub, workflow '.github/workflows/auto-pr.yml' sẽ tự động khởi tạo Pull Request vào 'main',
       gắn nhãn (labels) theo microservice và kích hoạt bộ 151/151 bài test CI!
    5. Nếu có biến môi trường $env:GITHUB_TOKEN, script sẽ gọi thẳng GitHub REST API để tạo PR ngay lập tức từ máy local.
.EXAMPLE
    .\scripts\New-AutoPullRequest.ps1 -BranchName "feat/seo-and-redis-upgrade" -Title "feat: upgrade SEO & Redis L2 caching"
#>
param(
    [string]$BranchName = "",
    [string]$Title = "",
    [string]$BaseBranch = "main"
)

$ErrorActionPreference = "Stop"

$currentBranch = (git rev-parse --abbrev-ref HEAD).Trim()
if ([string]::IsNullOrWhiteSpace($BranchName)) {
    if ($currentBranch -eq $BaseBranch) {
        $timestamp = Get-Date -Format "yyyyMMdd-HHmm"
        $BranchName = "feat/update-$timestamp"
        Write-Host "-> Đang tạo và chuyển sang nhánh mới: $BranchName" -ForegroundColor Cyan
        git checkout -b $BranchName
    } else {
        $BranchName = $currentBranch
    }
} elseif ($currentBranch -ne $BranchName) {
    git checkout -B $BranchName
}

$status = git status --porcelain
if (-not [string]::IsNullOrWhiteSpace($status)) {
    if ([string]::IsNullOrWhiteSpace($Title)) {
        $Title = "feat: automated update on $BranchName"
    }
    Write-Host "-> Đang tự động stage và commit thay đổi: $Title" -ForegroundColor Green
    git add -A
    git commit -m $Title
}

if ([string]::IsNullOrWhiteSpace($Title)) {
    $Title = (git log -1 --pretty=format:"%s").Trim()
}

Write-Host "-> Đang đẩy nhánh '$BranchName' lên origin..." -ForegroundColor Cyan
git push -u origin $BranchName

$remoteUrl = (git remote get-url origin).Trim()
if ($remoteUrl -match "github\.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+)(\.git)?$") {
    $owner = $Matches["owner"]
    $repo = $Matches["repo"]

    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
        Write-Host "-> Phát hiện GITHUB_TOKEN, đang tạo Pull Request trực tiếp qua GitHub REST API..." -ForegroundColor Yellow
        $headers = @{
            "Authorization" = "Bearer $($env:GITHUB_TOKEN)"
            "Accept"        = "application/vnd.github+json"
            "X-GitHub-Api-Version" = "2022-11-28"
        }
        $payload = @{
            title = $Title
            head  = $BranchName
            base  = $BaseBranch
            body  = "Automated Pull Request created by `scripts/New-AutoPullRequest.ps1` from branch `$BranchName` into `$BaseBranch`."
        } | ConvertTo-Json

        try {
            $pr = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/pulls" -Method Post -Headers $headers -Body $payload -ContentType "application/json"
            Write-Host "✓ Đã tạo Pull Request thành công: $($pr.html_url)" -ForegroundColor Green
            return
        } catch {
            Write-Host "! PR có thể đã tồn tại hoặc đang được xử lý bởi GitHub Actions." -ForegroundColor Yellow
        }
    }

    Write-Host "✓ Đã push thành công! GitHub Actions (.github/workflows/auto-pr.yml) sẽ tự động mở & gắn nhãn Pull Request tại:" -ForegroundColor Green
    Write-Host "  https://github.com/$owner/$repo/pulls" -ForegroundColor Cyan
}
