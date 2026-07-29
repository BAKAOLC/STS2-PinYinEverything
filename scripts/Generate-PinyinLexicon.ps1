param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\Resources\PinyinLexicon.tsv.br'),
    [string]$OverridePath = (Join-Path $PSScriptRoot '..\Resources\PinyinOverrides.tsv')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$characterCommit = '923b108dc5d45dee061324c011b478fb649f8b73'
$phraseCommit = 'cee0ed6e6e4898580cafd2bd5e3723e20b214aa0'
$characterUri = "https://raw.githubusercontent.com/mozillazg/pinyin-data/$characterCommit/pinyin.txt"
$phraseUri = "https://raw.githubusercontent.com/mozillazg/phrase-pinyin-data/$phraseCommit/pinyin.txt"
$characterSha256 = '621F8CA9EFF8519F47E2B17B564FD318161E13BCA07EEA8C8E04993CD5D3B52E'
$phraseSha256 = 'DCC769607C220B312FEA3E71CB63421298B4B891B1F7356A95AB58F2C96FFF81'

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('sts2-pinyin-lexicon-' + [guid]::NewGuid().ToString('N'))
$characterPath = Join-Path $tempRoot 'characters.txt'
$phrasePath = Join-Path $tempRoot 'phrases.txt'
$resolvedOutputPath = [IO.Path]::GetFullPath($OutputPath)
$resolvedOverridePath = [IO.Path]::GetFullPath($OverridePath)

function Remove-ToneMarks {
    param([Parameter(Mandatory)][string]$Value)

    $normalized = $Value.Normalize([Text.NormalizationForm]::FormD)
    $builder = [Text.StringBuilder]::new($normalized.Length)
    foreach ($character in $normalized.ToCharArray()) {
        if ($character -notin @([char]0x0304, [char]0x0301, [char]0x030C, [char]0x0300)) {
            [void]$builder.Append($character)
        }
    }

    return $builder.ToString().Normalize([Text.NormalizationForm]::FormC)
}

function Convert-ToNumberedTone {
    param([Parameter(Mandatory)][string]$Value)

    $normalized = $Value.Normalize([Text.NormalizationForm]::FormD)
    $builder = [Text.StringBuilder]::new($normalized.Length + 1)
    $tone = 0

    foreach ($character in $normalized.ToCharArray()) {
        switch ([int]$character) {
            0x0304 {
                $tone = 1
                continue
            }
            0x0301 {
                $tone = 2
                continue
            }
            0x030C {
                $tone = 3
                continue
            }
            0x0300 {
                $tone = 4
                continue
            }
            0x0308 {
                if ($builder.Length -gt 0 -and $builder[$builder.Length - 1] -eq 'u') {
                    $builder[$builder.Length - 1] = 'v'
                }
                continue
            }
        }

        if ([Globalization.CharUnicodeInfo]::GetUnicodeCategory($character) -ne
            [Globalization.UnicodeCategory]::NonSpacingMark) {
            [void]$builder.Append($character)
        }
    }

    if ($tone -gt 0) {
        [void]$builder.Append($tone)
    }
    return $builder.ToString()
}

function Assert-FileHash {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Expected
    )

    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    if ($actual -ne $Expected) {
        throw "Hash mismatch for $Path. Expected $Expected, got $actual."
    }
}

try {
    [void](New-Item -ItemType Directory -Path $tempRoot)
    Invoke-WebRequest -Uri $characterUri -OutFile $characterPath
    Invoke-WebRequest -Uri $phraseUri -OutFile $phrasePath
    Assert-FileHash -Path $characterPath -Expected $characterSha256
    Assert-FileHash -Path $phrasePath -Expected $phraseSha256

    $characters = [Collections.Generic.SortedDictionary[int, string[]]]::new()
    foreach ($line in [IO.File]::ReadLines($characterPath)) {
        if ($line -notmatch '^U\+([0-9A-F]+):\s*([^#]+)') {
            continue
        }

        $codePoint = [Convert]::ToInt32($Matches[1], 16)
        $toneMarks = (($Matches[2].Trim() -split ',')[0]).Trim()
        $characters[$codePoint] = @(
            (Remove-ToneMarks -Value $toneMarks),
            $toneMarks,
            (Convert-ToNumberedTone -Value $toneMarks)
        )
    }

    $phrases = [Collections.Generic.SortedDictionary[string, string[]]]::new(
        [StringComparer]::Ordinal)
    foreach ($line in [IO.File]::ReadLines($phrasePath)) {
        if ($line.StartsWith('#') -or $line -notmatch '^(.+?):\s*(.+)$') {
            continue
        }

        $phrase = $Matches[1].Trim()
        $toneMarkTokens = foreach ($token in ($Matches[2].Trim() -split '\s+')) {
            ($token -split ',')[0]
        }
        $plainTokens = foreach ($token in $toneMarkTokens) {
            Remove-ToneMarks -Value $token
        }
        $toneNumberTokens = foreach ($token in $toneMarkTokens) {
            Convert-ToNumberedTone -Value $token
        }
        $phrases[$phrase] = @(
            ($plainTokens -join ' '),
            ($toneMarkTokens -join ' '),
            ($toneNumberTokens -join ' ')
        )
    }

    $phraseOverrideCount = 0
    foreach ($line in [IO.File]::ReadLines($resolvedOverridePath)) {
        $trimmedLine = $line.Trim()
        if ($trimmedLine.Length -eq 0 -or $trimmedLine.StartsWith('#')) {
            continue
        }

        $columns = $line -split "`t", 2
        if ($columns.Length -ne 2 -or
            [string]::IsNullOrWhiteSpace($columns[0]) -or
            [string]::IsNullOrWhiteSpace($columns[1])) {
            throw "Invalid pinyin override line: $line"
        }

        $phrase = $columns[0].Trim()
        $toneMarkTokens = $columns[1].Trim() -split '\s+'
        $plainTokens = foreach ($token in $toneMarkTokens) {
            Remove-ToneMarks -Value $token
        }
        $toneNumberTokens = foreach ($token in $toneMarkTokens) {
            Convert-ToNumberedTone -Value $token
        }
        $phrases[$phrase] = @(
            ($plainTokens -join ' '),
            ($toneMarkTokens -join ' '),
            ($toneNumberTokens -join ' ')
        )
        $phraseOverrideCount++
    }

    $outputDirectory = Split-Path -Parent $resolvedOutputPath
    [void](New-Item -ItemType Directory -Force -Path $outputDirectory)

    $fileStream = [IO.File]::Create($resolvedOutputPath)
    try {
        $brotliStream = [IO.Compression.BrotliStream]::new(
            $fileStream,
            [IO.Compression.CompressionLevel]::Optimal,
            $false)
        try {
            $writer = [IO.StreamWriter]::new($brotliStream, [Text.UTF8Encoding]::new($false))
            try {
                foreach ($entry in $characters.GetEnumerator()) {
                    $writer.WriteLine(
                        "C`t{0:X}`t{1}`t{2}`t{3}",
                        $entry.Key,
                        $entry.Value[0],
                        $entry.Value[1],
                        $entry.Value[2])
                }
                foreach ($entry in $phrases.GetEnumerator()) {
                    $writer.WriteLine(
                        "P`t{0}`t{1}`t{2}`t{3}",
                        $entry.Key,
                        $entry.Value[0],
                        $entry.Value[1],
                        $entry.Value[2])
                }
            }
            finally {
                $writer.Dispose()
            }
        }
        finally {
            $brotliStream.Dispose()
        }
    }
    finally {
        $fileStream.Dispose()
    }

    Write-Output "Generated $resolvedOutputPath"
    Write-Output "Character readings: $($characters.Count)"
    Write-Output "Phrase readings: $($phrases.Count)"
    Write-Output "Phrase overrides: $phraseOverrideCount"
}
finally {
    $resolvedTempRoot = [IO.Path]::GetFullPath($tempRoot)
    $systemTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if ($resolvedTempRoot.StartsWith($systemTempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTempRoot)) {
        Remove-Item -LiteralPath $resolvedTempRoot -Recurse -Force
    }
}
