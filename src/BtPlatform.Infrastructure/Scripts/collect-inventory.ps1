# Salt okunur envanter betiği: sistemde hiçbir şeyi değiştirmez, sadece JSON yazar.
# Girdi almaz (kullanıcıdan gelen hiçbir metin komuta karışmaz).
$ErrorActionPreference = 'Stop'
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8

    $cs   = Get-CimInstance Win32_ComputerSystem
    $os   = Get-CimInstance Win32_OperatingSystem
    $bios = Get-CimInstance Win32_BIOS
    $cpu  = Get-CimInstance Win32_Processor | Select-Object -First 1
    $disk = Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" | Measure-Object -Property Size -Sum

    # Varsayılan ağ geçidi olan, bağlı ilk ağ kartı
    $net = Get-NetIPConfiguration |
        Where-Object { $_.IPv4DefaultGateway -ne $null -and $_.NetAdapter.Status -eq 'Up' } |
        Select-Object -First 1
    if (-not $net) { throw 'Bağlı bir ağ kartı bulunamadı.' }

    # Kurulu yazılımlar (Programlar ve Özellikler listesi, kayıt defterinden OKUNUR)
    $keys = @(
        'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
    )
    $apps = Get-ItemProperty -Path $keys -ErrorAction SilentlyContinue |
        Where-Object { $_.DisplayName -and $_.SystemComponent -ne 1 } |
        ForEach-Object {
            [pscustomobject]@{
                name      = [string]$_.DisplayName
                version   = [string]$_.DisplayVersion
                publisher = [string]$_.Publisher
            }
        } |
        Sort-Object name -Unique |
        Select-Object -First 100

    $result = [ordered]@{
        hostname     = $env:COMPUTERNAME
        manufacturer = [string]$cs.Manufacturer
        model        = [string]$cs.Model
        serialNumber = [string]$bios.SerialNumber
        processor    = ([string]$cpu.Name).Trim()
        ramGb        = [math]::Round($cs.TotalPhysicalMemory / 1GB)
        diskGb       = [math]::Round($disk.Sum / 1GB)
        osName       = [string]$os.Caption
        ipAddress    = [string]$net.IPv4Address[0].IPAddress
        macAddress   = [string]$net.NetAdapter.LinkLayerAddress
        apps         = @($apps)
    }

    $result | ConvertTo-Json -Depth 4 -Compress
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}