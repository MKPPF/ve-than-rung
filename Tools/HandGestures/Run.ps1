param(
    [ValidateSet('setup', 'prepare', 'train', 'collect', 'play', 'test')]
    [string]$Action = 'play',
    [string]$Subject = 'p01',
    [ValidateSet('Left', 'Right')][string]$Hand = 'Right',
    [int]$Camera = 0,
    [ValidateSet('Auto', 'Left', 'Right')][string]$MoveHand = 'Auto',
    [ValidateSet('both', 'point', 'palm')][string]$MovementStyle = 'both'
)
$ErrorActionPreference = 'Stop'
$pythonPath = Join-Path $PSScriptRoot '.venv/Scripts/python.exe'
if ($Action -eq 'setup') {
    if (-not (Test-Path -LiteralPath $pythonPath)) {
        python -m venv (Join-Path $PSScriptRoot '.venv')
        if ($LASTEXITCODE -ne 0) { throw 'Cannot create Python venv. Install Python 3.11-3.13.' }
    }
    & $pythonPath -m pip install -r (Join-Path $PSScriptRoot 'requirements.txt')
    exit $LASTEXITCODE
}
if (-not (Test-Path -LiteralPath $pythonPath)) { throw 'Run -Action setup first.' }
switch ($Action) {
    'prepare' { & $pythonPath (Join-Path $PSScriptRoot 'run_pipeline.py') prepare }
    'train' {
        $dataPaths = @((Join-Path $PSScriptRoot 'data/hagrid.csv'))
        $webcamPath = Join-Path $PSScriptRoot 'data/webcam.csv'
        if (Test-Path -LiteralPath $webcamPath) { $dataPaths += $webcamPath }
        & $pythonPath (Join-Path $PSScriptRoot 'run_pipeline.py') train --data @dataPaths
    }
    'collect' { & $pythonPath (Join-Path $PSScriptRoot 'run_pipeline.py') collect --subject $Subject --hand $Hand --camera $Camera }
    'play' { & $pythonPath (Join-Path $PSScriptRoot 'run_pipeline.py') play --camera $Camera --move-hand $MoveHand --movement-style $MovementStyle }
    'test' { & $pythonPath -m unittest discover -s $PSScriptRoot -p 'test_*.py' -v }
}
exit $LASTEXITCODE
