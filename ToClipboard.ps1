<#
.SYNOPSIS
    Recursively dump files beneath a directory into the Windows clipboard.

.DESCRIPTION
    Walks the directory tree rooted at –Path (default: current dir).  
    For every file that **is not excluded** *and*, if –Include is supplied,
    **matches one of the specified extensions**, it writes:

        <relative path>
        <file contents>

    to a .NET StringBuilder and finally copies the whole string to the
    clipboard using System.Windows.Forms.Clipboard.

    * No binary‑file detection is performed.  
      Every file is read as bytes and converted to text via –FileEncoding
      (default UTF‑8) – invalid byte sequences are replaced, as per .NET’s
      default decoder‑fallback behaviour.

.PARAMETER Path
    Root directory.  Must exist and be a folder.  Defaults to cwd.

.PARAMETER Exclude
    One or more path fragments or wildcard patterns evaluated against each
    file’s *relative* path.  If any pattern matches, that file is skipped.

.PARAMETER Include
    One or more file‑name extensions (with leading dot, e.g. “.cs”).  
    If supplied, only files whose Extension equals (case‑insensitive) one of
    the entries are processed.  When omitted, **all** extensions are allowed.

.PARAMETER FileEncoding
    Encoding used to turn raw bytes into text.  Defaults to UTF‑8.

.EXAMPLE
    PS> .\Copy-AllFilesToClipboard.ps1 -Exclude '.git','/bin/' `
                                       -Include '.ps1','.md'
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateScript({ Test-Path $_ -PathType 'Container' })]
    [string]$Path = (Get-Location).Path,

    [Parameter()]
    [string[]]$Exclude = @(),

    [Parameter()]
    [string[]]$Include = @(),

    [Parameter()]
    [System.Text.Encoding]$FileEncoding = [System.Text.Encoding]::UTF8

)

#region ----- Ensure STA for clipboard ----------------------------------------
if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') {
    # Relaunch this script in an STA PowerShell so Clipboard works.
    Write-Host "Relaunching in STA PowerShell..."
    
    powershell.exe -NoProfile -NoLogo -STA -ExecutionPolicy Bypass `
                   -File $PSCommandPath @PSBoundParameters
    return
}
#endregion --------------------------------------------------------------------

Add-Type -AssemblyName System.Windows.Forms

$rootPath = (Resolve-Path -LiteralPath $Path).ProviderPath
$builder  = [System.Text.StringBuilder]::new(1024)

$allFiles       = Get-ChildItem -LiteralPath $rootPath -File -Recurse -ErrorAction Stop
$processedCount = 0

foreach ($file in $allFiles) {

    # Relative path (for readability and pattern matching)
    $relativePath = $file.FullName.Substring($rootPath.Length).TrimStart('\','/')

    # ------------- Exclude filter ------------------------------------------
    if ($Exclude.Count -gt 0) {
        foreach ($pattern in $Exclude) {
            if ($relativePath -like "*$pattern*") { continue 2 }  # skip this file
        }
    }

    # ------------- Include filter (extension allow‑list) --------------------
    if ($Include.Count -gt 0 -and (-not ($Include -contains $file.Extension))) {
        continue
    }

    # ------------- Append path and contents ---------------------------------
    $builder.AppendLine($relativePath) > $null

    Write-Host $file.FullName
    Write-Host "  $($file.Length) bytes" -ForegroundColor DarkGray  


    $bytes  = [System.IO.File]::ReadAllBytes($file.FullName)
    $text   = $FileEncoding.GetString($bytes)

    $builder.AppendLine($text) > $null
    $builder.AppendLine()      > $null   # blank line between files

    $processedCount++
}

[System.Windows.Forms.Clipboard]::SetText($builder.ToString())

Write-Host "Copied $processedCount file(s) to clipboard (`"$($builder.Length)`" characters)."
