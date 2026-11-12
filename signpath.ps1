
param(
    [Parameter(Mandatory)]
    [string]$InputFile
)

$ErrorActionPreference = "Stop"

# Install SignPath module if necessary
if (!(Get-Module -ListAvailable -Name SignPath)) {
    Install-Module SignPath -Scope CurrentUser -Force -AllowClobber
}

Import-Module SignPath

$outputFile = "$InputFile.signed"

# Submit file and wait for signed result
Submit-SigningRequest `
    -InputArtifactPath $InputFile `
    -OutputArtifactPath $outputFile `
    -OrganizationId $env:SIGNPATH_ORGANIZATION_ID `
    -ApiToken $env:SIGNPATH_API_TOKEN `
    -ProjectSlug $env:SIGNPATH_PROJECT_SLUG `
    -SigningPolicySlug $env:SIGNPATH_SIGNING_POLICY_SLUG `
    -WaitForCompletion `
    -WaitForCompletionTimeoutInSeconds 1800 `
    -Force

if (!(Test-Path -LiteralPath $outputFile)) {
    throw "SignPath failed to produce signed file: $InputFile"
}

# Replace original with signed file
Move-Item -LiteralPath $outputFile -Destination $InputFile -Force
