# ==========================================================
# Prueba automática del CRUD de TimeBank CON TOKEN Y ROLES
# Requisito: la API corriendo en otra terminal (dotnet run)
# Uso: powershell -ExecutionPolicy Bypass -File tests\probar-api.ps1
# Administrador inicial (lo crea la migración AddRoles): admin@timebank.com / Admin2026*
# ==========================================================
$api = "http://localhost:5070/api"
$s = Get-Date -Format "HHmmss"   # sufijo para no repetir datos si se corre varias veces
$ok = 0; $fallos = 0

function Probar($num, $titulo, $metodo, $ruta, $body, $esperado, $token) {
    $params = @{ Uri = "$api$ruta"; Method = $metodo; ContentType = "application/json; charset=utf-8"; UseBasicParsing = $true }
    if ($body) { $params.Body = [System.Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 5)) }
    if ($token) { $params.Headers = @{ Authorization = "Bearer $token" } }
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
        try {
            $json = $contenido | ConvertFrom-Json
            $m = $json.message
            if (-not "$m".Trim() -and $json.errors) { $m = ($json.errors.PSObject.Properties | ForEach-Object { $_.Value }) -join " " }
            if ("$m".Trim()) { Write-Host "         mensaje: $m" -ForegroundColor DarkGray }
        } catch {}
    }
    if ($contenido -and $code -lt 400) { try { return ($contenido | ConvertFrom-Json) } catch {} }
}

Write-Host ""
Write-Host "=== PRUEBAS DEL CRUD DE TIMEBANK (con token y roles) ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "--- Seguridad ---" -ForegroundColor Cyan
Probar 1 "Error: listar usuarios SIN token" GET "/users" $null 401 | Out-Null
$admin = Probar 2 "Login del Administrador" POST "/auth/login" @{ email = "admin@timebank.com"; password = "Admin2026*" } 200
$tAdmin = $admin.token

Write-Host "--- Usuarios y roles ---" -ForegroundColor Cyan
$ana   = Probar 3 "Registrar a Ana (rol Usuario, 2 horas)" POST "/auth/register" @{ firstName = "Ana"; lastName = "Pérez"; email = "ana$s@mail.com"; password = "Ana12345*" } 201
$pedro = Probar 4 "Registrar a Pedro" POST "/auth/register" @{ firstName = "Pedro"; lastName = "López"; email = "pedro$s@mail.com"; password = "Pedro123#" } 201
Probar 5 "Error: contraseña débil (sin mayúscula ni carácter especial)" POST "/auth/register" @{ firstName = "Luis"; lastName = "Mora"; email = "luis$s@mail.com"; password = "clave123" } 400 | Out-Null
$tAna   = (Probar 6 "Login de Ana" POST "/auth/login" @{ email = "ana$s@mail.com"; password = "Ana12345*" } 200).token
$tPedro = (Probar 7 "Login de Pedro" POST "/auth/login" @{ email = "pedro$s@mail.com"; password = "Pedro123#" } 200).token
Probar 8 "Error: un Usuario no puede listar usuarios" GET "/users" $null 403 $tAna | Out-Null
Probar 9 "Administrador busca usuarios" GET "/users?search=$s" $null 200 $tAdmin | Out-Null
Probar 10 "Administrador ve los roles" GET "/roles" $null 200 $tAdmin | Out-Null
$carlos = Probar 11 "Administrador crea a Carlos como Administrador" POST "/users" @{ firstName = "Carlos"; lastName = "Ruiz"; email = "carlos$s@mail.com"; password = "Carlos2026!"; roles = @("Administrador") } 201 $tAdmin
Probar 12 "Administrador cambia a Carlos al rol Usuario" PUT "/users/$($carlos.id)/roles" @{ roles = @("Usuario") } 200 $tAdmin | Out-Null
Probar 13 "Administrador edita a Ana" PUT "/users/$($ana.id)" @{ firstName = "Ana María"; lastName = "Pérez"; email = "ana$s@mail.com"; phone = "0999999999"; isActive = $true } 204 $tAdmin | Out-Null

Write-Host "--- Categorías ---" -ForegroundColor Cyan
Probar 14 "Ana lista categorías (con token)" GET "/categories" $null 200 $tAna | Out-Null
Probar 15 "Error: Ana (Usuario) crea categoría" POST "/categories" @{ name = "Otra $s" } 403 $tAna | Out-Null
$cat = Probar 16 "Administrador crea categoría" POST "/categories" @{ name = "Idiomas $s"; description = "Clases de inglés y otros" } 201 $tAdmin
Probar 17 "Error: categoría repetida" POST "/categories" @{ name = "idiomas $s" } 400 $tAdmin | Out-Null
Probar 18 "Administrador edita categoría" PUT "/categories/$($cat.id)" @{ id = $cat.id; name = "Idiomas y música $s"; description = "Idiomas e instrumentos" } 204 $tAdmin | Out-Null

Write-Host "--- Servicios ---" -ForegroundColor Cyan
$srv1 = Probar 19 "Ana publica: clases de inglés" POST "/services" @{ userId = $ana.id; categoryId = $cat.id; title = "Clases de inglés"; description = "Nivel básico"; estimatedHours = 2 } 201 $tAna
$srv2 = Probar 20 "Pedro publica: reparación de PC" POST "/services" @{ userId = $pedro.id; categoryId = 2; title = "Reparación de computadoras"; description = "Formateo"; estimatedHours = 1.5 } 201 $tPedro
Probar 21 "Error: Pedro publica a nombre de Ana" POST "/services" @{ userId = $ana.id; categoryId = 2; title = "Falso"; description = "x"; estimatedHours = 1 } 403 $tPedro | Out-Null
Probar 22 "Filtrar servicios por categoría y texto" GET "/services?categoryId=$($cat.id)&search=ingl" $null 200 $tPedro | Out-Null
Probar 23 "Ana edita su servicio" PUT "/services/$($srv1.id)" @{ userId = $ana.id; categoryId = $cat.id; title = "Clases de inglés intermedio"; description = "Conversación"; estimatedHours = 2; isActive = $true } 204 $tAna | Out-Null
Probar 24 "Error: Pedro edita el servicio de Ana" PUT "/services/$($srv1.id)" @{ userId = $ana.id; categoryId = $cat.id; title = "Cambiado"; description = "x"; estimatedHours = 2; isActive = $true } 403 $tPedro | Out-Null

Write-Host "--- Solicitudes e intercambio de horas ---" -ForegroundColor Cyan
Probar 25 "Error: Ana pide su propio servicio" POST "/servicerequests" @{ serviceId = $srv1.id; requesterId = $ana.id; requestedHours = 1 } 400 $tAna | Out-Null
Probar 26 "Error: saldo insuficiente" POST "/servicerequests" @{ serviceId = $srv1.id; requesterId = $pedro.id; requestedHours = 10 } 400 $tPedro | Out-Null
Probar 27 "Error: Ana pide a nombre de Pedro" POST "/servicerequests" @{ serviceId = $srv2.id; requesterId = $pedro.id; requestedHours = 1 } 403 $tAna | Out-Null
$req = Probar 28 "Pedro pide clases de inglés (Pendiente)" POST "/servicerequests" @{ serviceId = $srv1.id; requesterId = $pedro.id; requestedHours = 2; message = "Quiero aprender" } 201 $tPedro
Probar 29 "Error: Pedro acepta (no es quien ofrece)" PUT "/servicerequests/$($req.id)/status" @{ status = "Aceptada" } 403 $tPedro | Out-Null
Probar 30 "Error: completar sin aceptar" PUT "/servicerequests/$($req.id)/status" @{ status = "Completada" } 400 $tPedro | Out-Null
Probar 31 "Ana acepta la solicitud" PUT "/servicerequests/$($req.id)/status" @{ status = "Aceptada" } 200 $tAna | Out-Null
Probar 32 "Error: eliminar solicitud ya aceptada" DELETE "/servicerequests/$($req.id)" $null 400 $tPedro | Out-Null
Probar 33 "Pedro marca Completada (transfiere horas)" PUT "/servicerequests/$($req.id)/status" @{ status = "Completada" } 200 $tPedro | Out-Null
$ana2   = Probar 34 "Ver saldo de Ana" GET "/users/$($ana.id)" $null 200 $tAna
$pedro2 = Probar 35 "Ver saldo de Pedro" GET "/users/$($pedro.id)" $null 200 $tPedro
Write-Host ("         Saldo de Ana: {0} horas (antes 2)   Saldo de Pedro: {1} horas (antes 2)" -f $ana2.hoursBalance, $pedro2.hoursBalance) -ForegroundColor White
Probar 36 "Historial de horas de Ana" GET "/timetransactions" $null 200 $tAna | Out-Null

Write-Host "--- Valoraciones ---" -ForegroundColor Cyan
Probar 37 "Pedro valora a Ana con 5" POST "/reviews" @{ serviceRequestId = $req.id; reviewerId = $pedro.id; rating = 5; comment = "Excelente" } 201 $tPedro | Out-Null
Probar 38 "Error: valoración fuera de rango" POST "/reviews" @{ serviceRequestId = $req.id; reviewerId = $ana.id; rating = 9 } 400 $tAna | Out-Null
Probar 39 "Error: Ana valora a nombre de Pedro" POST "/reviews" @{ serviceRequestId = $req.id; reviewerId = $pedro.id; rating = 4 } 403 $tAna | Out-Null

Write-Host "--- Eliminaciones ---" -ForegroundColor Cyan
Probar 40 "Error: eliminar categoría con servicios" DELETE "/categories/$($cat.id)" $null 400 $tAdmin | Out-Null
Probar 41 "Pedro elimina su servicio sin solicitudes" DELETE "/services/$($srv2.id)" $null 204 $tPedro | Out-Null
Probar 42 "Error: Ana (Usuario) elimina un usuario" DELETE "/users/$($pedro.id)" $null 403 $tAna | Out-Null
Probar 43 "Administrador elimina usuario con historial (se desactiva)" DELETE "/users/$($pedro.id)" $null 200 $tAdmin | Out-Null

Write-Host ""
Write-Host ("RESULTADO: {0} pruebas correctas, {1} con problemas" -f $ok, $fallos) -ForegroundColor $(if ($fallos -eq 0) { "Green" } else { "Red" })
Write-Host ""
