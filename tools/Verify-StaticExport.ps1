param(
    [Parameter(Mandatory)]
    [string]$InputPath,

    [Parameter(Mandatory)]
    [string]$DecompilerPath,

    [string]$RepositoryDirectory = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$expectedHash = '77EA21DB08F93892C139A5A5DD5C1265444D070FDCA61F3F02E2E6ED9AF4FFE7'
if ((Get-FileHash -LiteralPath $InputPath -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'The input hash differs from the reviewed sample.'
}
[void][System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $DecompilerPath).Path)
$sourceDirectory = Join-Path $RepositoryDirectory 'src'
$stream = [System.IO.File]::OpenRead((Resolve-Path -LiteralPath $InputPath).Path)
$reader = $null
try {
    $reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($reader)
    $typeCount = 0
    foreach ($handle in $metadata.TypeDefinitions) {
        $definition = $metadata.GetTypeDefinition($handle)
        $name = $metadata.GetString($definition.Name)
        if (-not $definition.GetDeclaringType().IsNil -or $name.StartsWith('<')) {
            continue
        }
        $namespace = $metadata.GetString($definition.Namespace)
        $relativePath = Join-Path $namespace ($name + '.cs')
        if (-not (Test-Path -LiteralPath (Join-Path $sourceDirectory $relativePath))) {
            throw ('Missing exported type ' + $namespace + '.' + $name)
        }
        $typeCount++
    }

    $resourceRva = $reader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress
    [byte[]]$resourceBytes = $reader.GetSectionData($resourceRva).GetContent()
    $resourceCount = 0
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        foreach ($handle in $metadata.ManifestResources) {
            $resource = $metadata.GetManifestResource($handle)
            if (-not $resource.Implementation.IsNil) {
                throw 'An external manifest resource needs separate verification.'
            }
            $name = $metadata.GetString($resource.Name)
            $offset = [int]$resource.Offset
            $length = [System.BitConverter]::ToInt32($resourceBytes, $offset)
            $digest = $sha.ComputeHash($resourceBytes, $offset + 4, $length)
            $expected = [System.BitConverter]::ToString($digest).Replace('-', '')
            $actual = (Get-FileHash -LiteralPath (Join-Path $sourceDirectory $name) -Algorithm SHA256).Hash
            if ($actual -ne $expected) {
                throw ('Resource bytes differ for ' + $name)
            }
            $resourceCount++
        }
    } finally {
        $sha.Dispose()
    }
} finally {
    if ($reader) { $reader.Dispose() }
    $stream.Dispose()
}

# Reuse the Roslyn version bundled with PowerShell to avoid assembly conflicts.
[void][System.Reflection.Assembly]::LoadFrom((Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll'))
[void][System.Reflection.Assembly]::LoadFrom((Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll'))
$parseOptions = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithLanguageVersion(
    [Microsoft.CodeAnalysis.CSharp.LanguageVersion]::CSharp11
)
$sourceFiles = @(Get-ChildItem -LiteralPath $sourceDirectory -Recurse -File -Filter '*.cs')
foreach ($file in $sourceFiles) {
    $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText(
        [System.IO.File]::ReadAllText($file.FullName), $parseOptions, $file.Name,
        [System.Text.Encoding]::UTF8, [System.Threading.CancellationToken]::None
    )
    $errors = @($tree.GetDiagnostics([System.Threading.CancellationToken]::None) | Where-Object {
        $_.Severity.ToString() -eq 'Error'
    })
    if ($errors.Count -gt 0) {
        throw ('C# syntax errors in ' + $file.Name)
    }
}

$textFiles = @(Get-ChildItem -LiteralPath $RepositoryDirectory -Recurse -File | Where-Object {
    $_.Extension -in '.cs', '.xaml', '.json', '.csproj', '.md', '.ps1', '.manifest'
})
$credentialPatterns = @(
    'CAPT(?:-[A-Z0-9]{4}){4}',
    'AKIA[A-Z0-9]{16}',
    'gh[pousr]_[A-Za-z0-9_]{20,}',
    'github_pat_[A-Za-z0-9_]{20,}',
    '-----BEGIN [A-Z ]*PRIVATE KEY-----',
    '(?i)[A-Z]:[\\/]Users[\\/]'
)
foreach ($file in $textFiles) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    foreach ($pattern in $credentialPatterns) {
        foreach ($match in [regex]::Matches($text, $pattern)) {
            if ($match.Value -eq 'CAPT-XXXX-XXXX-XXXX-XXXX') { continue }
            # Report the file, keeping the potential credential out of the console.
            throw ('Publication scan needs review in ' + $file.Name)
        }
    }
}
$binaryFiles = @(Get-ChildItem -LiteralPath $RepositoryDirectory -Recurse -File | Where-Object {
    $_.Extension -in '.exe', '.dll', '.pdb', '.key', '.dat', '.pfx', '.p12'
})
if ($binaryFiles.Count -gt 0) {
    throw 'Executable samples or private application state exist in the publication directory.'
}

[pscustomobject]@{
    SampleHash = $expectedHash
    TopLevelTypesPresent = $typeCount
    ResourcesByteIdentical = $resourceCount
    CSharpFilesParsed = $sourceFiles.Count
    PublicationScan = 'Passed selected credential and personal-path patterns'
    SampleExecution = 'Not performed'
    Build = 'Not performed'
} | Format-List
