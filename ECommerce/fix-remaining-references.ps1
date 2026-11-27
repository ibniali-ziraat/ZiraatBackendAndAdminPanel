# PowerShell Script to fix remaining SimplCommerce references in cshtml and other view files
param(
    [switch]$WhatIf = $false
)

$rootPath = "d:\CodeStrix Code\ZiratApiProject\SimpleCommerce"
Set-Location $rootPath

Write-Host "Fixing remaining SimplCommerce references in view files..." -ForegroundColor Green

# File types to update (focusing on view files that were missed)
$fileExtensions = @('*.cshtml', '*.razor', '*.html', '*.htm', '*.xml', '*.config')

$filesToUpdate = Get-ChildItem -Path $rootPath -Recurse -File -Include $fileExtensions | 
    Where-Object { $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\.vs\*" }

$totalFiles = $filesToUpdate.Count
$currentFile = 0
$updatedCount = 0

Write-Host "Found $totalFiles files to check..." -ForegroundColor Cyan

foreach ($file in $filesToUpdate) {
    $currentFile++
    
    try {
        $content = Get-Content -Path $file.FullName -Raw -ErrorAction Stop
        
        if ($content -match 'SimplCommerce') {
            $newContent = $content -replace 'SimplCommerce', 'ECommerce'
            
            Write-Host "  [$currentFile/$totalFiles] Updating: $($file.FullName)" -ForegroundColor Yellow
            $updatedCount++
            
            if (-not $WhatIf) {
                Set-Content -Path $file.FullName -Value $newContent -NoNewline -Force
            }
        }
    }
    catch {
        Write-Host "  Warning: Could not process $($file.FullName): $_" -ForegroundColor Red
    }
}

Write-Host "`nCompleted! Updated $updatedCount files." -ForegroundColor Green

if ($WhatIf) {
    Write-Host "This was a dry run. Run without -WhatIf to apply changes." -ForegroundColor Yellow
}
