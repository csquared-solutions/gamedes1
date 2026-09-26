param(
    [string]$OutputRoot = (Join-Path $PSScriptRoot '..\assets\audio')
)

$sampleRate = 22050
$twoPi = 2.0 * [Math]::PI

function Write-MonoWave {
    param(
        [string]$Path,
        [double[]]$Samples
    )

    $stream = [IO.File]::Open($Path, [IO.FileMode]::Create)
    $writer = [IO.BinaryWriter]::new($stream)
    try {
        $dataSize = $Samples.Length * 2
        $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF'))
        $writer.Write(36 + $dataSize)
        $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVE'))
        $writer.Write([Text.Encoding]::ASCII.GetBytes('fmt '))
        $writer.Write(16)
        $writer.Write([int16]1)
        $writer.Write([int16]1)
        $writer.Write($sampleRate)
        $writer.Write($sampleRate * 2)
        $writer.Write([int16]2)
        $writer.Write([int16]16)
        $writer.Write([Text.Encoding]::ASCII.GetBytes('data'))
        $writer.Write($dataSize)

        foreach ($sample in $Samples) {
            $clamped = [Math]::Max(-1.0, [Math]::Min(1.0, $sample))
            $writer.Write([int16]($clamped * 32767))
        }
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

[IO.Directory]::CreateDirectory($OutputRoot) | Out-Null

$moth = [double[]]::new([int]($sampleRate * 0.48))
for ($index = 0; $index -lt $moth.Length; $index++) {
    $time = $index / $sampleRate
    $envelope = [Math]::Sin([Math]::PI * $index / $moth.Length) * [Math]::Exp(-1.7 * $time)
    $flutter = 0.3 + 0.7 * [Math]::Abs([Math]::Sin($twoPi * 19 * $time))
    $moth[$index] = 0.32 * $envelope * $flutter * (
        [Math]::Sin($twoPi * 510 * $time) +
        0.42 * [Math]::Sin($twoPi * 760 * $time)
    )
}

$gutter = [double[]]::new([int]($sampleRate * 0.68))
for ($index = 0; $index -lt $gutter.Length; $index++) {
    $time = $index / $sampleRate
    $envelope = [Math]::Exp(-3.0 * $time) * (0.35 + 0.65 * [Math]::Abs([Math]::Sin($twoPi * 7 * $time)))
    $gutter[$index] = 0.38 * $envelope * (
        [Math]::Sin($twoPi * 128 * $time) +
        0.33 * [Math]::Sin($twoPi * 83 * $time)
    )
}

Write-MonoWave -Path (Join-Path $OutputRoot 'moth-wake.wav') -Samples $moth
Write-MonoWave -Path (Join-Path $OutputRoot 'low-wick-gutter.wav') -Samples $gutter

$lightOut = [double[]]::new([int]($sampleRate * 0.9))
for ($index = 0; $index -lt $lightOut.Length; $index++) {
    $time = $index / $sampleRate
    $snuffEnvelope = [Math]::Exp(-18.0 * $time)
    $thudEnvelope = [Math]::Exp(-5.2 * $time)
    $hiss = [Math]::Sin($twoPi * 1860.0 * $time + 7.0 * [Math]::Sin($twoPi * 93.0 * $time))
    $thud = [Math]::Sin($twoPi * (94.0 - 38.0 * $time) * $time)
    $lightOut[$index] = 0.34 * $snuffEnvelope * $hiss + 0.5 * $thudEnvelope * $thud
}

Write-MonoWave -Path (Join-Path $OutputRoot 'light-out.wav') -Samples $lightOut

$ambienceDuration = 24.0
$ambience = [double[]]::new([int]($sampleRate * $ambienceDuration))
$droneFrequencies = @(42.6666666667, 64.0, 85.3333333333, 213.3333333333)
for ($index = 0; $index -lt $ambience.Length; $index++) {
    $time = $index / $sampleRate
    $slowBreath = 0.78 + 0.22 * [Math]::Sin($twoPi * $time / 12.0)
    $airMovement = 0.74 + 0.26 * [Math]::Sin($twoPi * $time / 8.0 + 1.1)
    $sample = 0.055 * $slowBreath * [Math]::Sin($twoPi * $droneFrequencies[0] * $time)
    $sample += 0.031 * [Math]::Sin($twoPi * $droneFrequencies[1] * $time + 0.7)
    $sample += 0.018 * [Math]::Sin($twoPi * $droneFrequencies[2] * $time + 1.8)
    $sample += 0.019 * $airMovement * [Math]::Sin($twoPi * 160.0 * $time + 0.2)
    $sample += 0.012 * $slowBreath * [Math]::Sin($twoPi * 240.0 * $time + 1.4)
    $sample += 0.007 * $airMovement * [Math]::Sin($twoPi * 360.0 * $time + 2.1)

    foreach ($strike in @(5.0, 12.5, 19.0)) {
        $age = $time - $strike
        if ($age -ge 0.0 -and $age -lt 1.8) {
            $decay = [Math]::Exp(-4.2 * $age)
            $sample += 0.11 * $decay * (
                [Math]::Sin($twoPi * 320.0 * $age) +
                0.46 * [Math]::Sin($twoPi * 476.0 * $age)
            )
        }
    }

    $ambience[$index] = $sample
}

Write-MonoWave -Path (Join-Path $OutputRoot 'basement-ambience.wav') -Samples $ambience
