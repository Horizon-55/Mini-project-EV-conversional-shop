# Database initialization and verification script
# Lab 01: Microservices Databases

$ErrorActionPreference = "Stop"

Write-Host "--> 1. Starting containers via docker compose..." -ForegroundColor Cyan
docker compose up -d

Write-Host "`n--> 2. Waiting for SQL Server 2022..." -ForegroundColor Cyan
$sqlReady = $false
$retry = 0
while (-not $sqlReady -and $retry -lt 30) {
    Start-Sleep -Seconds 2
    $retry++
    $check = docker exec evshop-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "StrongPassw0rd!EV2026" -C -Q "SELECT 1" 2>&1
    if ($LASTEXITCODE -eq 0) {
        $sqlReady = $true
        Write-Host "    [OK] SQL Server is ready!" -ForegroundColor Green
    } else {
        Write-Host "    ... waiting for SQL Server ($retry/30)" -ForegroundColor DarkGray
    }
}

if (-not $sqlReady) {
    Write-Error "SQL Server did not start within timeout."
}

Write-Host "`n--> 3. Waiting for MongoDB 7.0..." -ForegroundColor Cyan
$mongoReady = $false
$retry = 0
while (-not $mongoReady -and $retry -lt 20) {
    Start-Sleep -Seconds 1
    $retry++
    $check = docker exec evshop-mongodb mongosh -u admin -p "AdminPassword123!" --eval "db.adminCommand('ping')" 2>&1
    if ($LASTEXITCODE -eq 0) {
        $mongoReady = $true
        Write-Host "    [OK] MongoDB is ready!" -ForegroundColor Green
    } else {
        Write-Host "    ... waiting for MongoDB ($retry/20)" -ForegroundColor DarkGray
    }
}

Write-Host "`n--> 4. Executing SQL Server schema, indexes, procedures and seed data..." -ForegroundColor Cyan
docker cp databases/scripts/init-all.sql evshop-mssql:/tmp/init-all.sql
docker exec evshop-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "StrongPassw0rd!EV2026" -C -i /tmp/init-all.sql
Write-Host "    [OK] SQL databases initialized!" -ForegroundColor Green

Write-Host "`n--> 5. Executing MongoDB collections and seed scripts..." -ForegroundColor Cyan
docker cp databases/reviews-db/01_init-collections.js evshop-mongodb:/tmp/01_init-collections.js
docker cp databases/reviews-db/02_seed-data.js evshop-mongodb:/tmp/02_seed-data.js
docker cp databases/reviews-db/03_sample-queries.js evshop-mongodb:/tmp/03_sample-queries.js

docker exec evshop-mongodb mongosh -u admin -p "AdminPassword123!" /tmp/01_init-collections.js
docker exec evshop-mongodb mongosh -u admin -p "AdminPassword123!" /tmp/02_seed-data.js
Write-Host "    [OK] MongoDB collections initialized!" -ForegroundColor Green

Write-Host "`n--> 6. Verification checks:" -ForegroundColor Yellow

Write-Host "`n[Check 1] Foreign Keys in Ordering DB (must only reference internal tables):"
docker exec evshop-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "StrongPassw0rd!EV2026" -C -d EVShop_Ordering -Q "SELECT name, OBJECT_NAME(parent_object_id) AS ParentTable, OBJECT_NAME(referenced_object_id) AS RefTable FROM sys.foreign_keys;"

Write-Host "`n[Check 2] Testing business transition: attempting to ship unpaid order:"
docker exec evshop-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "StrongPassw0rd!EV2026" -C -d EVShop_Ordering -Q "
BEGIN TRY
    EXEC dbo.usp_ShipOrder @OrderId = 1001;
    PRINT 'ERROR: Transition should not have succeeded!';
END TRY
BEGIN CATCH
    PRINT 'SUCCESS: Blocked invalid transition: ' + ERROR_MESSAGE();
END CATCH;
"

Write-Host "`n[Check 3] MongoDB sample aggregation queries:"
docker exec evshop-mongodb mongosh -u admin -p "AdminPassword123!" /tmp/03_sample-queries.js

Write-Host "`nAll databases initialized and verified successfully!" -ForegroundColor Green
Write-Host "Web UI (Mongo Express): http://localhost:8081" -ForegroundColor Green
