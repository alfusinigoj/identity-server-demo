$ErrorActionPreference = 'Stop'

if($args.Length -eq 0 || $args[0] -eq '')
{
    throw "pass argument as the image version suffix"
}

$versionSuffix=$args[0]
$registry = 'index.docker.io/ajaganathan'
$imageName = 'identity-server'
$imageTag = "1.0.0-$versionSuffix"

$fullImageName = "$($imageName):$($imageTag)"
$fullImageTag = "$registry/$fullImageName"

docker run --env ASPNETCORE_ENVIRONMENT=Development -p 8091:8080 $fullImageTag