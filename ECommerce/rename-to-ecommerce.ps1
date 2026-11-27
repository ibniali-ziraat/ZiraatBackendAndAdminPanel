# PowerShell Script to Rename ECommerce to ECommerce
# This script renames directories and files from ECommerce to ECommerce

param(
    [switch]$WhatIf = $false
)

$rootPath = "d:\CodeStrix Code\ZiratApiProject\SimpleCommerce"
Set-Location $rootPath

Write-Host "Starting rename operation from ECommerce to ECommerce..." -ForegroundColor Green
Write-Host "Root path: $rootPath" -ForegroundColor Cyan

if ($WhatIf) {
    Write-Host "Running in WhatIf mode - no changes will be made" -ForegroundColor Yellow
}

# Function to rename directories
function Rename-Directories {
    Write-Host "`nRenaming directories..." -ForegroundColor Green
    
    # Get all directories with ECommerce in the name, sorted by depth (deepest first)
    $directories = Get-ChildItem -Path $rootPath -Recurse -Directory -Filter "*ECommerce*" | 
        Sort-Object { $_.FullName.Split('\').Count } -Descending
    
    foreach ($dir in $directories) {
        $newName = $dir.Name -replace 'ECommerce', 'ECommerce'
        $newPath = Join-Path $dir.Parent.FullName $newName
        
        if ($dir.Name -ne $newName) {
            Write-Host "  Directory: $($dir.FullName) -> $newPath" -ForegroundColor Cyan
            if (-not $WhatIf) {
                Rename-Item -Path $dir.FullName -NewName $newName -Force
            }
        }
    }
}

# Function to rename files
function Rename-Files {
    Write-Host "`nRenaming files..." -ForegroundColor Green
    
    # Get all files with ECommerce in the name
    $files = Get-ChildItem -Path $rootPath -Recurse -File -Filter "*ECommerce*"
    
    foreach ($file in $files) {
        $newName = $file.Name -replace 'ECommerce', 'ECommerce'
        $newPath = Join-Path $file.DirectoryName $newName
        
        if ($file.Name -ne $newName) {
            Write-Host "  File: $($file.FullName) -> $newPath" -ForegroundColor Cyan
            if (-not $WhatIf) {
                Rename-Item -Path $file.FullName -NewName $newName -Force
            }
        }
    }
}

# Function to update file contents
function Update-FileContents {
    Write-Host "`nUpdating file contents..." -ForegroundColor Green
    
    # File types to update
    $fileExtensions = @('*.cs', '*.csproj', '*.sln', '*.json', '*.props', '*.md', '*.yml', '*.yaml', '*.sh', '*.bat', '*.ps1')
    
    $filesToUpdate = Get-ChildItem -Path $rootPath -Recurse -File -Include $fileExtensions | 
        Where-Object { $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\.vs\*" }
    
    $totalFiles = $filesToUpdate.Count
    $currentFile = 0
    
    foreach ($file in $filesToUpdate) {
        $currentFile++
        
        try {
            $content = Get-Content -Path $file.FullName -Raw -ErrorAction Stop
            
            if ($content -match 'ECommerce') {
                $newContent = $content -replace 'ECommerce', 'ECommerce'
                
                Write-Host "  [$currentFile/$totalFiles] Updating: $($file.FullName)" -ForegroundColor Cyan
                
                if (-not $WhatIf) {
                    Set-Content -Path $file.FullName -Value $newContent -NoNewline -Force
                }
            }
        }
        catch {
            Write-Host "  Warning: Could not process $($file.FullName): $_" -ForegroundColor Yellow
        }
    }
}

# Execute the rename operations
try {
    # Step 1: Rename directories (deepest first to avoid path issues)
    Rename-Directories
    
    # Step 2: Rename files
    Rename-Files
    
    # Step 3: Update file contents
    Update-FileContents
    
    Write-Host "`nRename operation completed successfully!" -ForegroundColor Green
    
    if ($WhatIf) {
        Write-Host "`nThis was a dry run. Run without -WhatIf to apply changes." -ForegroundColor Yellow
    }
    else {
        Write-Host "`nNext steps:" -ForegroundColor Yellow
        Write-Host "1. Review the changes" -ForegroundColor White
        Write-Host "2. Try building the solution: dotnet build ECommerce.sln" -ForegroundColor White
        Write-Host "3. Run tests if available: dotnet test ECommerce.sln" -ForegroundColor White
    }
}
catch {
    Write-Host "`nError during rename operation: $_" -ForegroundColor Red
    exit 1
}
