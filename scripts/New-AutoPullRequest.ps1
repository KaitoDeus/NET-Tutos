<#
.SYNOPSIS
    Automatically creates a feature branch, commits pending changes, pushes to GitHub, and triggers automated Pull Request creation for NET-Tutos.
.DESCRIPTION
    1. If currently on 'main', automatically creates and switches to a timestamped feature branch (e.g. feat/update-20261103-2010).
    2. Automatically runs git add -A and git commit if uncommitted changes exist.
    3. Automatically runs git push -u origin <branchName>.
    4. Pushing to GitHub triggers '.github/workflows/auto-pr.yml' to open/update the Pull Request into 'main',
       attach bounded-context labels, and run the 151/151 CI test suite.
    5. If $env:GITHUB_TOKEN is set, calls the GitHub REST API directly from the local machine to open the PR immediately.
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
