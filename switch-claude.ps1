# switch-claude.ps1
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("local", "cloud")]
    [string]$Mode,
    
    [Parameter(Mandatory=$false)]
    [string]$SessionId = $null,
    
    [Parameter(Mandatory=$false)]
    [switch]$DangerouslySkipPermissions
)

function Show-LocalModelHelp {
    Write-Host "`n[ERROR] Missing required environment variables for local model" -ForegroundColor Red
    Write-Host "`nPlease set the following environment variables:" -ForegroundColor Yellow
    Write-Host "`n  CLAUDE_LOCAL_HOST    - IP address of your local model server"
    Write-Host "  CLAUDE_LOCAL_PORT    - Port number of your local model server"
    Write-Host "`nExample (current session only):" -ForegroundColor Cyan
    Write-Host '  $env:CLAUDE_LOCAL_HOST = "10.0.0.84"'
    Write-Host '  $env:CLAUDE_LOCAL_PORT = "11434"'
    Write-Host "`nTo set permanently, add to your PowerShell profile:" -ForegroundColor Cyan
    Write-Host "  notepad `$PROFILE"
    Write-Host "`nThen add these lines:" -ForegroundColor Green
    Write-Host '  $env:CLAUDE_LOCAL_HOST = "10.0.0.84"'
    Write-Host '  $env:CLAUDE_LOCAL_PORT = "11434"'
    Write-Host ""
    exit 1
}

if ($Mode -eq "local") {
    Write-Host "[LOCAL] Switching to LOCAL model (qwen3-coder)..." -ForegroundColor Cyan
    
    # Validate required environment variables
    if (-not $env:CLAUDE_LOCAL_HOST) {
        Show-LocalModelHelp
    }
    
    if (-not $env:CLAUDE_LOCAL_PORT) {
        Show-LocalModelHelp
    }
    
    # Construct the base URL
    $baseUrl = "http://${env:CLAUDE_LOCAL_HOST}:${env:CLAUDE_LOCAL_PORT}"
    Write-Host "[INFO] Connecting to: $baseUrl" -ForegroundColor Gray
    
    $env:ANTHROPIC_BASE_URL = $baseUrl
    $env:ANTHROPIC_AUTH_TOKEN = "dummy-token"
    $env:CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC = "1"
    
    $claudeArgs = @("--model", "qwen3-coder")
    
    if ($DangerouslySkipPermissions) {
        $claudeArgs += "--dangerously-skip-permissions"
    }
    
    if ($SessionId) {
        $claudeArgs += @("--resume", $SessionId)
        Write-Host "[INFO] Resuming session: $SessionId" -ForegroundColor Yellow
    }
    
    & claude $claudeArgs
    
} elseif ($Mode -eq "cloud") {
    Write-Host "[CLOUD] Switching to CLOUD model..." -ForegroundColor Cyan
    
    # Remove local model environment variables
    Remove-Item Env:\ANTHROPIC_BASE_URL -ErrorAction SilentlyContinue
    Remove-Item Env:\ANTHROPIC_AUTH_TOKEN -ErrorAction SilentlyContinue
    Remove-Item Env:\CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC -ErrorAction SilentlyContinue
    
    $claudeArgs = @()
    
    if ($SessionId) {
        $claudeArgs += @("--resume", $SessionId)
        Write-Host "[INFO] Resuming session: $SessionId" -ForegroundColor Yellow
    }
    
    & claude $claudeArgs
}