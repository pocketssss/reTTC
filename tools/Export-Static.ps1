param(
    [Parameter(Mandatory)]
    [string]$InputPath,

    [Parameter(Mandatory)]
    [string]$DecompilerPath,

    [string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'src')
)

$ErrorActionPreference = 'Stop'
$expectedHash = '77EA21DB08F93892C139A5A5DD5C1265444D070FDCA61F3F02E2E6ED9AF4FFE7'
$inputFile = (Resolve-Path -LiteralPath $InputPath).Path
$decompilerFile = (Resolve-Path -LiteralPath $DecompilerPath).Path
if ((Get-FileHash -LiteralPath $inputFile -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'The input hash differs from the sample covered by this analysis.'
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw 'Choose a new output directory. Existing exports are never overwritten.'
}

# Load the analysis library. The sample is read through PE metadata APIs.
[void][System.Reflection.Assembly]::LoadFrom($decompilerFile)
$streamOptions = [System.Reflection.PortableExecutable.PEStreamOptions]::PrefetchEntireImage
$metadataOptions = [System.Reflection.Metadata.MetadataReaderOptions]::ApplyWindowsRuntimeProjections
$pe = [ICSharpCode.Decompiler.Metadata.PEFile]::new($inputFile, $streamOptions, $metadataOptions)
try {
    $resolver = [ICSharpCode.Decompiler.Metadata.UniversalAssemblyResolver]::new(
        $inputFile, $false, '.NETFramework,Version=v4.0', $null,
        $streamOptions, $metadataOptions
    )
    $project = [ICSharpCode.Decompiler.CSharp.ProjectDecompiler.WholeProjectDecompiler]::new($resolver)
    $project.Settings.ThrowOnAssemblyResolveErrors = $false
    $project.Settings.UseSdkStyleProjectFormat = $true
    $project.MaxDegreeOfParallelism = 2
    [void][System.IO.Directory]::CreateDirectory($OutputDirectory)
    $project.DecompileProject($pe, $OutputDirectory, [System.Threading.CancellationToken]::None)
} finally {
    $pe.Dispose()
}

Write-Output ('Static export completed in ' + [System.IO.Path]::GetFullPath($OutputDirectory))
