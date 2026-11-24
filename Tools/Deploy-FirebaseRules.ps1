[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$project = 'cars-velocity-game'
$package = 'com.carsstudentteam.carsvelocity'
$cli = (Get-Command firebase.cmd).Source
$root = Split-Path -Parent $PSScriptRoot
$database = "projects/$project/databases/(default)/documents"
$firestore = "https://firestore.googleapis.com/v1/$database"
$identity = 'https://identitytoolkit.googleapis.com/v1/accounts'

function Invoke-FirebaseCli {
    & $cli @args
    if ($LASTEXITCODE -ne 0) { throw "Firebase CLI failed (exit $LASTEXITCODE)." }
}

function Invoke-Identity($operation, $body) {
    $uri = "${identity}:${operation}?key=$([Uri]::EscapeDataString($apiKey))"
    try {
        Invoke-RestMethod -Uri $uri -Method Post -ContentType 'application/json' -Body ($body | ConvertTo-Json -Compress)
    }
    catch {
        $response = $_.Exception.Response
        if ($null -eq $response) { throw "Firebase Auth $operation failed before receiving an HTTP response." }
        throw "Firebase Auth $operation failed (HTTP $([int]$response.StatusCode))."
    }
}

function New-AnonymousAccount {
    $account = Invoke-Identity 'signUp' @{ returnSecureToken = $true }
    if ($account.localId -cnotmatch '^[A-Za-z0-9_-]{1,128}$' -or -not $account.idToken) {
        throw 'Firebase Auth did not return a valid temporary account.'
    }
    $account
}

function Invoke-Firestore($method, $uri, $token, $body) {
    $headers = @{}
    if ($token) { $headers.Authorization = "Bearer $token" }
    $options = @{ Method = $method; Uri = $uri; Headers = $headers }
    if ($null -ne $body) {
        $options.ContentType = 'application/json'
        $options.Body = $body | ConvertTo-Json -Depth 16 -Compress
    }
    Invoke-RestMethod @options
}

function Assert-Denied($method, $uri, $token, $body, $label) {
    try {
        $null = Invoke-Firestore $method $uri $token $body
    }
    catch {
        $response = $_.Exception.Response
        if ($null -ne $response -and [int]$response.StatusCode -eq 403) { return }
        throw
    }
    throw "Security rules allowed $label."
}

function New-Write($name, $fields, $timestampField) {
    @{
        update = @{ name = $name; fields = $fields }
        updateTransforms = @(@{ fieldPath = $timestampField; setToServerValue = 'REQUEST_TIME' })
    }
}

$ownerAccount = $null
$otherAccount = $null
$profilePath = $null
$resultPath = $null
$writeAttempted = $false
$cleanupFailed = $false
Push-Location -LiteralPath $root
try {
    $sdk = Get-Content -LiteralPath 'Assets/google-services.json' -Raw | ConvertFrom-Json
    $client = $sdk.client | Where-Object { $_.client_info.android_client_info.package_name -ceq $package } | Select-Object -First 1
    $apiKey = ($client.api_key | Select-Object -First 1).current_key
    if ($sdk.project_info.project_id -cne $project -or -not $apiKey) {
        throw 'google-services.json does not match the expected Firebase project and Android app.'
    }
    $deployment = Get-Content -LiteralPath 'firebase.json' -Raw | ConvertFrom-Json
    if ($deployment.firestore.rules -cne 'Docs/firestore.rules' -or -not (Test-Path -LiteralPath 'Docs/firestore.rules' -PathType Leaf)) {
        throw 'firebase.json does not point to the expected Firestore rules file.'
    }

    Invoke-FirebaseCli deploy --only 'firestore:rules' --project $project --non-interactive

    $ownerAccount = New-AnonymousAccount
    $otherAccount = New-AnonymousAccount
    $uid = $ownerAccount.localId
    if ($uid -ceq $otherAccount.localId) { throw 'The smoke test requires two distinct accounts.' }
    $profilePath = "players/$uid"
    $runId = [Guid]::NewGuid().ToString('N')
    $resultPath = "$profilePath/results/$runId"
    $finishedUtc = [DateTime]::UtcNow.ToString('O')
    $profileJson = @{
        schemaVersion = 1; playerId = $uid; coins = 120
        speedLevel = 1; accelerationLevel = 2; handlingLevel = 3
        best = @(@{ key = 'Race_GreenValley_2'; score = 500; time = 12; completed = $true })
        pending = @(); rewardedRuns = @($runId)
    } | ConvertTo-Json -Depth 6 -Compress
    $profileFields = @{
        playerId = @{ stringValue = $uid }; coins = @{ integerValue = '120' }
        profileJson = @{ stringValue = $profileJson }
        upgrades = @{ mapValue = @{ fields = @{
            speed = @{ integerValue = '1' }; acceleration = @{ integerValue = '2' }; handling = @{ integerValue = '3' }
        } } }
    }
    $resultFields = @{
        playerId = @{ stringValue = $uid }; score = @{ integerValue = '500' }
        finishedUtc = @{ stringValue = $finishedUtc }; mode = @{ stringValue = 'Race' }
        track = @{ stringValue = 'GreenValley' }; level = @{ integerValue = '2' }
        distance = @{ doubleValue = 400 }; elapsed = @{ doubleValue = 12 }
        position = @{ integerValue = '1' }; won = @{ booleanValue = $true }
        earnedCoins = @{ integerValue = '120' }
    }
    $profileWrite = New-Write "$database/$profilePath" $profileFields 'updatedAt'
    $resultWrite = New-Write "$database/$resultPath" $resultFields 'createdAt'
    $commit = "${firestore}:commit"
    $writeAttempted = $true
    $null = Invoke-Firestore Post $commit $ownerAccount.idToken @{ writes = @($profileWrite, $resultWrite) }

    $savedProfile = Invoke-Firestore Get "$firestore/$profilePath" $ownerAccount.idToken
    $result = Invoke-Firestore Get "$firestore/$resultPath" $ownerAccount.idToken
    $upgrades = $savedProfile.fields.upgrades.mapValue.fields
    if ($savedProfile.fields.playerId.stringValue -cne $uid -or
        $savedProfile.fields.profileJson.stringValue -cne $profileJson -or
        $savedProfile.fields.coins.integerValue -ne '120' -or
        $upgrades.speed.integerValue -ne '1' -or $upgrades.acceleration.integerValue -ne '2' -or
        $upgrades.handling.integerValue -ne '3' -or -not $savedProfile.fields.updatedAt.timestampValue -or
        $result.fields.playerId.stringValue -cne $uid -or
        $result.fields.score.integerValue -ne '500' -or $result.fields.earnedCoins.integerValue -ne '120' -or
        $result.fields.mode.stringValue -cne 'Race' -or $result.fields.track.stringValue -cne 'GreenValley' -or
        $result.fields.level.integerValue -ne '2' -or -not $result.fields.createdAt.timestampValue -or
        $result.fields.distance.doubleValue -ne 400 -or $result.fields.elapsed.doubleValue -ne 12 -or
        $result.fields.position.integerValue -ne '1' -or $result.fields.won.booleanValue -ne $true -or
        [DateTimeOffset]$result.fields.finishedUtc.stringValue -ne [DateTimeOffset]$finishedUtc) {
        throw 'The Firestore read-back did not match the test data.'
    }

    Assert-Denied Get "$firestore/$profilePath" $null $null 'an unauthenticated profile read'
    Assert-Denied Get "$firestore/$resultPath" $null $null 'an unauthenticated result read'
    Assert-Denied Get "$firestore/$profilePath" $otherAccount.idToken $null 'another player to read the profile'
    Assert-Denied Get "$firestore/$resultPath" $otherAccount.idToken $null 'another player to read a result'
    Assert-Denied Post $commit $null @{ writes = @($profileWrite) } 'an unauthenticated write'
    Assert-Denied Post $commit $otherAccount.idToken @{ writes = @($profileWrite) } 'another player to write the profile'
    Assert-Denied Post $commit $otherAccount.idToken @{ writes = @($resultWrite) } 'another player to write a result'
    $profileFields.coins = @{ integerValue = '-1' }
    Assert-Denied Post $commit $ownerAccount.idToken @{ writes = @($profileWrite) } 'a negative coin balance'
    $profileFields.coins = @{ integerValue = '120' }
    $profileFields.upgrades.mapValue.fields.speed = @{ integerValue = '6' }
    Assert-Denied Post $commit $ownerAccount.idToken @{ writes = @($profileWrite) } 'an out-of-range upgrade'
    Write-Host 'PASS: Firebase anonymous sign-in, live Firestore write/read, and access restrictions.'
}
finally {
    if ($writeAttempted) {
        foreach ($path in @($resultPath, $profilePath)) {
            try {
                Invoke-FirebaseCli firestore:delete $path --shallow --force --project $project --non-interactive
            }
            catch {
                $cleanupFailed = $true
                Write-Warning "Could not remove temporary Firestore document $path."
            }
        }
    }
    foreach ($account in @($otherAccount, $ownerAccount)) {
        if ($null -eq $account) { continue }
        try { $null = Invoke-Identity 'delete' @{ idToken = $account.idToken } }
        catch {
            $cleanupFailed = $true
            Write-Warning "Could not remove temporary Firebase Auth user $($account.localId)."
        }
    }
    Pop-Location
}
if ($cleanupFailed) { throw 'Smoke test finished with incomplete cleanup.' }
