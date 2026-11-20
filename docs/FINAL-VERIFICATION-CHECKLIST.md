# Final Verification Checklist — Run Before Submission

This checklist is intended for the developer's final local run. Use a fresh SQLite database if you want isolated results. Stop the API before deleting `src/PropertyShowingBookingApi/property-showings.db`.

## 1. Restore, build and automated tests

Run from the solution root in PowerShell:

```powershell
dotnet restore .\PropertyShowingBookingApi.sln
dotnet build .\PropertyShowingBookingApi.sln --no-restore
dotnet test .\PropertyShowingBookingApi.sln --no-build --logger "console;verbosity=normal"
```

Expected test count in this handover: 13. Record the actual build and test output in `docs/IMPLEMENTATION-LOG.md`. If restore/build/test fails, fix the first actionable error and rerun the full sequence.

## 2. Start the API

```powershell
dotnet run --project .\src\PropertyShowingBookingApi
```

Open the Swagger URL printed by the application and use the actual port shown by your terminal. The launch profile in this repository uses `https://localhost:54223` and `http://localhost:54224` when that profile is selected.

## 3. Duplicate active booking

1. Create a future slot with capacity 5.
2. Book 3 seats using `duplicate@example.com`.
3. Submit another booking for the same slot using `duplicate@example.com` and party size 1.
4. Expected: first request `200`; second request `409`.
5. List the slot. Expected seats remaining: 2 (the rejected duplicate must not consume another seat).

## 4. Cancellation and rebooking

1. Cancel the first booking before the slot starts using actor `renter`.
2. Expected: `200`, status `Cancelled`.
3. Retrieve `/bookings/{id}/history`. Expected entries: initial `Confirmed -> Confirmed`, then `Confirmed -> Cancelled` with the supplied actor/reason.
4. List the slot. Expected seats remaining: 5.
5. Book the same slot again using the same renter email, party size 2.
6. Expected: `200`, new booking ID, status `Confirmed`, seats remaining 3.

## 5. Owner attendance / no-show transitions

For each case, create a separate slot and booking. Set the slot start time a few minutes ahead, then wait until the start time before the successful transition. Do not change the machine clock.

- Owner requests `Attended` before the slot starts: expect `409`.
- After the slot starts, owner requests `Attended`: expect `200`, status `Attended`.
- Owner requests `NoShow` after another slot starts: expect `200`, status `NoShow`.
- Renter requests `Attended` or `NoShow`: expect `409`.
- Try any further transition from `Cancelled`, `Attended` or `NoShow`: expect `409`.
- Verify `Attended` and `NoShow` do not release seats. Only cancellation releases seats.
- Verify history records from/to state, timestamp, actor and reason.

## 6. Concurrency race (manual HTTP smoke test)

Create a fresh slot with capacity 5. Run the following from PowerShell 5.1, replacing the slot ID. The certificate callback is scoped to each background job because the local development certificate may not be trusted by PowerShell jobs.

```powershell
$slotId = "YOUR_SLOT_ID"
$url = "https://localhost:54223/slots/$slotId/bookings"

$job1 = Start-Job -ArgumentList $url, "racer1@example.com" -ScriptBlock {
    param($url, $email)
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
    $body = @{ renterEmail = $email; partySize = 3 } | ConvertTo-Json -Compress
    try {
        $r = Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json" -Body $body
        [PSCustomObject]@{ Renter = $email; Status = 200; Body = ($r | ConvertTo-Json -Compress) }
    }
    catch {
        $status = "No HTTP response"
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        [PSCustomObject]@{ Renter = $email; Status = $status; Error = $_.Exception.Message }
    }
}

$job2 = Start-Job -ArgumentList $url, "racer2@example.com" -ScriptBlock {
    param($url, $email)
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
    $body = @{ renterEmail = $email; partySize = 3 } | ConvertTo-Json -Compress
    try {
        $r = Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json" -Body $body
        [PSCustomObject]@{ Renter = $email; Status = 200; Body = ($r | ConvertTo-Json -Compress) }
    }
    catch {
        $status = "No HTTP response"
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        [PSCustomObject]@{ Renter = $email; Status = $status; Error = $_.Exception.Message }
    }
}

Wait-Job $job1, $job2 | Out-Null
Receive-Job $job1 | Format-List
Receive-Job $job2 | Format-List
Remove-Job $job1, $job2
```

Expected: exactly one `200` and one `409`; a subsequent `GET /slots` should show 2 seats remaining. Either renter may win. This validates one race run only; repeat it if time permits.

## 7. Final review

- Confirm no real credentials, personal renter data or local database files are committed.
- Confirm README run/test instructions match the local result.
- Confirm `docs/IMPLEMENTATION-LOG.md` records actual output rather than expected output.
- Confirm actor authorization is clearly documented as a take-home limitation (the request body is not secure authentication).
- Commit the source, tests and documentation only; exclude `.vs`, `bin`, `obj`, `TestResults` and `*.db` files.
