# ==========================================================
# Prueba automática del CRUD de TimeBank
# Requisito: la API corriendo en otra terminal (dotnet run)
# Uso: powershell -ExecutionPolicy Bypass -File tests\probar-api.ps1
# ==========================================================
$api = "http://localhost:5070/api"
$s = Get-Date -Format "HHmmss"   # sufijo para no repetir datos si se corre varias veces
$ok = 0; $fallos = 0

function Probar($num, $titulo, $metodo, $ruta, $body, $esperado) {
    $params = @{ Uri = "$api$ruta"; Method = $metodo; ContentType = "application/json; charset=utf-8"; UseBasicParsing = $true }
    if ($body) { $params.Body = [System.Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 5)) }
    try {
        $r = Invoke-WebRequest @params
        $code = [int]$r.StatusCode
        $contenido = $r.Content
    } catch {
        $code = [int]$_.Exception.Response.StatusCode
        $contenido = $_.ErrorDetails.Message
    }
    $bien = ($code -eq $esperado)
    if ($bien) { $script:ok++; $color = "Green"; $marca = "OK " } else { $script:fallos++; $color = "Red"; $marca = "MAL" }
    Write-Host ("{0} {1,2}. [{2}] {3} {4}" -f $marca, $num, $code, $metodo.PadRight(6), $titulo) -ForegroundColor $color
    if ($code -ge 400 -and $contenido) {
        try { $m = ($contenido | ConvertFrom-Json).message; if ($m) { Write-Host "         mensaje: $m" -ForegroundColor DarkGray } } catch {}
    }
    if ($contenido -and $code -lt 400) { try { return ($contenido | ConvertFrom-Json) } catch {} }
}

Write-Host ""
Write-Host "=== PRUEBAS DEL CRUD DE TIMEBANK ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "--- Categorías ---" -ForegroundColor Cyan
$cats = Probar 1 "Listar categorías" GET "/categories" $null 200
$cat  = Probar 2 "Crear categoría" POST "/categories" @{ name = "Idiomas $s"; description = "Clases de inglés y otros" } 201
Probar 3 "Error: categoría repetida" POST "/categories" @{ name = "idiomas $s" } 400 | Out-Null
Probar 4 "Editar categoría" PUT "/categories/$($cat.id)" @{ id = $cat.id; name = "Idiomas y música $s"; description = "Idiomas e instrumentos" } 204 | Out-Null

Write-Host "--- Usuarios ---" -ForegroundColor Cyan
$ana   = Probar 5 "Crear usuaria Ana (2 horas de bienvenida)" POST "/users" @{ firstName = "Ana"; lastName = "Pérez"; email = "ana$s@mail.com"; password = "123456" } 201
$pedro = Probar 6 "Crear usuario Pedro" POST "/users" @{ firstName = "Pedro"; lastName = "López"; email = "pedro$s@mail.com"; password = "123456" } 201
Probar 7 "Error: correo repetido" POST "/users" @{ firstName = "Otra"; lastName = "Ana"; email = "ana$s@mail.com"; password = "123456" } 400 | Out-Null
Probar 8 "Error: datos inválidos" POST "/users" @{ firstName = ""; lastName = "X"; email = "correo-malo"; password = "1" } 400 | Out-Null
Probar 9 "Buscar usuarios por texto" GET "/users?search=ana$s" $null 200 | Out-Null
Probar 10 "Editar usuaria Ana" PUT "/users/$($ana.id)" @{ firstName = "Ana María"; lastName = "Pérez"; email = "ana$s@mail.com"; phone = "0999999999"; isActive = $true } 204 | Out-Null

Write-Host "--- Servicios ---" -ForegroundColor Cyan
$srv1 = Probar 11 "Crear servicio: Ana ofrece inglés" POST "/services" @{ userId = $ana.id; categoryId = $cat.id; title = "Clases de inglés"; description = "Nivel básico"; estimatedHours = 2 } 201
$srv2 = Probar 12 "Crear servicio: Pedro ofrece reparación" POST "/services" @{ userId = $pedro.id; categoryId = 2; title = "Reparación de computadoras"; description = "Formateo"; estimatedHours = 1.5 } 201
Probar 13 "Filtrar servicios por categoría y texto" GET "/services?categoryId=$($cat.id)&search=ingl" $null 200 | Out-Null
Probar 14 "Editar servicio" PUT "/services/$($srv1.id)" @{ userId = $ana.id; categoryId = $cat.id; title = "Clases de inglés intermedio"; description = "Conversación"; estimatedHours = 2; isActive = $true } 204 | Out-Null

Write-Host "--- Solicitudes e intercambio de horas ---" -ForegroundColor Cyan
Probar 15 "Error: pedir mi propio servicio" POST "/servicerequests" @{ serviceId = $srv1.id; requesterId = $ana.id; requestedHours = 1 } 400 | Out-Null
Probar 16 "Error: saldo insuficiente" POST "/servicerequests" @{ serviceId = $srv1.id; requesterId = $pedro.id; requestedHours = 10 } 400 | Out-Null
$req = Probar 17 "Crear solicitud: Pedro pide inglés (Pendiente)" POST "/servicerequests" @{ serviceId = $srv1.id; requesterId = $pedro.id; requestedHours = 2; message = "Quiero aprender" } 201
Probar 18 "Error: completar sin aceptar" PUT "/servicerequests/$($req.id)/status" @{ status = "Completada" } 400 | Out-Null
Probar 19 "Aceptar solicitud" PUT "/servicerequests/$($req.id)/status" @{ status = "Aceptada" } 200 | Out-Null
Probar 20 "Error: eliminar solicitud ya aceptada" DELETE "/servicerequests/$($req.id)" $null 400 | Out-Null
Probar 21 "Completar solicitud (transfiere horas)" PUT "/servicerequests/$($req.id)/status" @{ status = "Completada" } 200 | Out-Null
$ana2   = Probar 22 "Ver saldo de Ana" GET "/users/$($ana.id)" $null 200
$pedro2 = Probar 23 "Ver saldo de Pedro" GET "/users/$($pedro.id)" $null 200
Write-Host ("         Saldo de Ana: {0} horas (antes 2)   Saldo de Pedro: {1} horas (antes 2)" -f $ana2.hoursBalance, $pedro2.hoursBalance) -ForegroundColor White
Probar 24 "Historial de horas de Ana" GET "/timetransactions?userId=$($ana.id)" $null 200 | Out-Null

Write-Host "--- Valoraciones ---" -ForegroundColor Cyan
Probar 25 "Pedro valora a Ana con 5" POST "/reviews" @{ serviceRequestId = $req.id; reviewerId = $pedro.id; rating = 5; comment = "Excelente" } 201 | Out-Null
Probar 26 "Error: valoración fuera de rango" POST "/reviews" @{ serviceRequestId = $req.id; reviewerId = $ana.id; rating = 9 } 400 | Out-Null

Write-Host "--- Eliminaciones ---" -ForegroundColor Cyan
Probar 27 "Error: eliminar categoría con servicios" DELETE "/categories/$($cat.id)" $null 400 | Out-Null
Probar 28 "Eliminar servicio sin solicitudes" DELETE "/services/$($srv2.id)" $null 204 | Out-Null
Probar 29 "Eliminar usuario con historial (se desactiva)" DELETE "/users/$($pedro.id)" $null 200 | Out-Null

Write-Host ""
Write-Host ("RESULTADO: {0} pruebas correctas, {1} con problemas" -f $ok, $fallos) -ForegroundColor $(if ($fallos -eq 0) { "Green" } else { "Red" })
Write-Host ""
