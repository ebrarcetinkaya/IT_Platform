# Komut metni betiğe GÖMÜLMEZ: BT_CMD ortam değişkeninden okunur.
# Önce PowerShell ayrıştırıcısıyla yapısal kontrol, geçerse çalıştırılır.
$ErrorActionPreference = 'Stop'
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $cmd = $env:BT_CMD
    if ([string]::IsNullOrWhiteSpace($cmd)) { throw 'Komut boş.' }

    $allowed = @('Get-Process', 'Get-Service', 'Get-HotFix', 'Get-CimInstance', 'Get-NetIPConfiguration',
        'Get-ComputerInfo', 'Test-Connection', 'Select-Object', 'Where-Object', 'Sort-Object',
        'Measure-Object', 'Format-Table', 'ConvertTo-Json')

    $tokens = $null; $errors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseInput($cmd, [ref]$tokens, [ref]$errors)
    if ($errors.Count -gt 0) { throw 'Reddedildi: komut sözdizimi hatalı.' }
    if ($ast.ParamBlock -or $ast.BeginBlock -or $ast.ProcessBlock -or $ast.DynamicParamBlock) { throw 'Reddedildi: desteklenmeyen yapı.' }

    $statements = @($ast.EndBlock.Statements)
    if ($statements.Count -eq 0) { throw 'Reddedildi: komut yok.' }

    foreach ($st in $statements) {
        if ($st -isnot [System.Management.Automation.Language.PipelineAst]) { throw 'Reddedildi: yalnızca boru hattı komutlarına izin var.' }
        foreach ($el in $st.PipelineElements) {
            if ($el -isnot [System.Management.Automation.Language.CommandAst]) { throw 'Reddedildi: yalnızca komutlara izin var.' }
            if ($el.InvocationOperator -ne 'Unknown') { throw 'Reddedildi: & ve . çağrıları yasak.' }
            if ($el.Redirections.Count -gt 0) { throw 'Reddedildi: yönlendirme yasak.' }

            $name = $el.GetCommandName()
            if ([string]::IsNullOrEmpty($name)) { throw 'Reddedildi: komut adı okunamadı.' }

            $rest = @($el.CommandElements | Select-Object -Skip 1)

            if ($name -eq 'ipconfig') {
                if ($rest.Count -ne 1 -or $rest[0].Extent.Text -ne '/all') { throw "Reddedildi: yalnızca 'ipconfig /all' çalışır." }
                continue
            }
            if ($allowed -notcontains $name) { throw "Reddedildi: '$name' izinli değil." }

            foreach ($ce in $rest) {
                if ($ce -is [System.Management.Automation.Language.CommandParameterAst]) {
                    if ($ce.Argument) { throw 'Reddedildi: -param:değer biçimi yasak.' }
                    continue
                }
                if ($ce -is [System.Management.Automation.Language.StringConstantExpressionAst] -or
                    $ce -is [System.Management.Automation.Language.ConstantExpressionAst]) { continue }
                if ($ce -is [System.Management.Automation.Language.ArrayLiteralAst]) {
                    foreach ($x in $ce.Elements) {
                        if ($x -isnot [System.Management.Automation.Language.StringConstantExpressionAst] -and
                            $x -isnot [System.Management.Automation.Language.ConstantExpressionAst]) { throw 'Reddedildi: desteklenmeyen argüman.' }
                    }
                    continue
                }
                throw 'Reddedildi: değişken, alt ifade ya da betik bloğu yasak.'
            }
        }
    }

    $sb = [scriptblock]::Create($cmd)
    $out = & $sb | Out-String -Width 200
    [Console]::Out.Write($out)
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
