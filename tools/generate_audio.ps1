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
        0.33 * [Math]::Sin($twoPi * 83 * $time) +
        0.45 * [Math]::Sin($twoPi * 256 * $time) +
        0.25 * [Math]::Sin($twoPi * 384 * $time)
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

# Action cues. Seeded noise keeps the output reproducible.
$random = [Random]::new(7)

$step = [double[]]::new([int]($sampleRate * 0.14))
$lowpass = 0.0
for ($index = 0; $index -lt $step.Length; $index++) {
    $time = $index / $sampleRate
    $lowpass += 0.18 * (($random.NextDouble() * 2.0 - 1.0) - $lowpass)
    $step[$index] = 1.6 * [Math]::Exp(-38.0 * $time) * $lowpass +
        0.35 * [Math]::Exp(-30.0 * $time) * [Math]::Sin($twoPi * 170.0 * $time)
}

Write-MonoWave -Path (Join-Path $OutputRoot 'step.wav') -Samples $step

$lamp = [double[]]::new([int]($sampleRate * 0.4))
for ($index = 0; $index -lt $lamp.Length; $index++) {
    $time = $index / $sampleRate
    $lamp[$index] = 0.3 * [Math]::Exp(-13.0 * $time) * (
        [Math]::Sin($twoPi * 1320.0 * $time) +
        0.6 * [Math]::Sin($twoPi * 1985.0 * $time) +
        0.3 * [Math]::Sin($twoPi * 2650.0 * $time)
    ) + 0.35 * [Math]::Exp(-25.0 * $time) * [Math]::Sin($twoPi * 150.0 * $time)
}

Write-MonoWave -Path (Join-Path $OutputRoot 'lamp-clink.wav') -Samples $lamp

$flask = [double[]]::new([int]($sampleRate * 0.62))
$phase = 0.0
for ($index = 0; $index -lt $flask.Length; $index++) {
    $time = $index / $sampleRate
    $sample = 0.0
    foreach ($start in @(0.0, 0.17, 0.34)) {
        $age = $time - $start
        if ($age -ge 0.0 -and $age -lt 0.14) {
            $frequency = 260.0 + 3200.0 * $age
            $sample += 0.6 * [Math]::Sin([Math]::PI * $age / 0.14) * [Math]::Sin($twoPi * $frequency * $age)
        }
    }

    $flask[$index] = $sample
}

Write-MonoWave -Path (Join-Path $OutputRoot 'flask-glug.wav') -Samples $flask

$flare = [double[]]::new([int]($sampleRate * 0.75))
$lowpass = 0.0
for ($index = 0; $index -lt $flare.Length; $index++) {
    $time = $index / $sampleRate
    $lowpass += 0.45 * (($random.NextDouble() * 2.0 - 1.0) - $lowpass)
    $envelope = [Math]::Min(1.0, $time / 0.06) * [Math]::Exp(-4.5 * $time)
    $crackle = if ($random.NextDouble() -lt 0.004) { $random.NextDouble() - 0.5 } else { 0.0 }
    $sweep = [Math]::Sin($twoPi * (500.0 + 900.0 * $time) * $time)
    $flare[$index] = $envelope * (0.55 * $lowpass + 0.22 * $sweep) + 0.8 * $crackle
}

Write-MonoWave -Path (Join-Path $OutputRoot 'flare-whoosh.wav') -Samples $flare

$clear = [double[]]::new([int]($sampleRate * 1.2))
$notes = @(523.25, 659.25, 783.99, 1046.5)
for ($index = 0; $index -lt $clear.Length; $index++) {
    $time = $index / $sampleRate
    $sample = 0.0
    for ($note = 0; $note -lt $notes.Length; $note++) {
        $age = $time - 0.11 * $note
        if ($age -ge 0.0) {
            $sample += 0.2 * [Math]::Exp(-3.6 * $age) * (
                [Math]::Sin($twoPi * $notes[$note] * $age) +
                0.3 * [Math]::Sin($twoPi * 2.0 * $notes[$note] * $age)
            )
        }
    }

    $clear[$index] = $sample
}

Write-MonoWave -Path (Join-Path $OutputRoot 'stair-clear.wav') -Samples $clear
