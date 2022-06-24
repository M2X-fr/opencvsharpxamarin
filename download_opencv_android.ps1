$tag = "4.6.0.20220608"
$version = "460"
$uriArray = @(
    "https://github.com/m2x/opencvsharpxamarin/releases/download/${tag}/opencv_build_install.zip"
)

function Download($uri, $outFile) {
    Write-Host "Downloading ${uri}"
    if (!(Test-Path $outFile)) {
        Invoke-WebRequest -Uri $uri -OutFile $outFile -ErrorAction Stop
    }
}

New-Item opencv_build_install -Type directory -Force -ErrorAction Stop | Out-Null
cd opencv_build_install

[Net.ServicePointManager]::SecurityProtocol = @([Net.SecurityProtocolType]::Tls, [Net.SecurityProtocolType]::Tls11, [Net.SecurityProtocolType]::Tls12)

foreach ($uri in $uriArray) {
    $outFile = [System.IO.Path]::GetFileName($uri)
    $outFileWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($uri)
    Download $uri $outFile
    Expand-Archive -Path $outFile -DestinationPath $outFileWithoutExtension -Force -ErrorAction Stop
    Remove-Item -Path $outFile -ErrorAction Stop
}

cd ..
