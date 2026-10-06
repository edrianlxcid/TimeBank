# ==========================================================
# Prueba automática del LOGIN de TimeBank (/api/auth)
# Requisito: la API corriendo en otra terminal (dotnet run)
# Uso: powershell -ExecutionPolicy Bypass -File tests\probar-login.ps1
# ==========================================================
$api = "http://localhost:5070/api"
$s = Get-Date -Format "HHmmss"   # sufijo para no repetir el correo si se corre varias veces
$correo = "laura$s@mail.com"
$ok = 0; $fallos = 0

function Probar($num, $titulo, $metodo, $ruta, $body, $esperado, $token, $agente) {
    $params = @{ Uri = "$api$ruta"; Method = $metodo; ContentType = "application/json; charset=utf-8"; UseBasicParsing = $true }
    if ($body) { $params.Body = [System.Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 5)) }
    if ($token) { $params.Headers = @{ Authorization = "Bearer $token" } }
    if ($agente) { $params.UserAgent = $agente }
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
    if ($contenido) {
        try { $m = ($contenido | ConvertFrom-Json).message; if ("$m".Trim()) { Write-Host "         mensaje: $m" -ForegroundColor DarkGray } } catch {}
    }
    if ($contenido -and $code -lt 400) { try { return ($contenido | ConvertFrom-Json) } catch {} }
}

Write-Host ""
Write-Host "=== PRUEBAS DEL LOGIN DE TIMEBANK ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "--- Registro ---" -ForegroundColor Cyan
Probar 1 "Registrar usuaria Laura (2 horas de bienvenida)" POST "/auth/register" @{ firstName = "Laura"; lastName = "Torres"; email = $correo; password = "Clave2026*"; phone = "0991112233" } 201 | Out-Null
Probar 2 "Error: correo ya registrado" POST "/auth/register" @{ firstName = "Laura"; lastName = "Torres"; email = $correo; password = "Clave2026*" } 409 | Out-Null
Probar 3 "Error: datos inválidos" POST "/auth/register" @{ firstName = ""; lastName = "T"; email = "correo-malo"; password = "123" } 400 | Out-Null
Probar 4 "Error: contraseña sin mayúscula ni carácter especial" POST "/auth/register" @{ firstName = "Luis"; lastName = "Mora"; email = "luis$s@mail.com"; password = "clave1234" } 400 | Out-Null

Write-Host "--- Inicio de sesión ---" -ForegroundColor Cyan
Probar 5 "Error: contraseña incorrecta" POST "/auth/login" @{ email = $correo; password = "equivocada" } 401 | Out-Null
$login = Probar 6 "Login correcto (devuelve el token)" POST "/auth/login" @{ email = $correo; password = "Clave2026*" } 200 $null "Postman"
$token = $login.token
Write-Host ("         token: {0}..." -f $token.Substring(0, 40)) -ForegroundColor White
Write-Host ("         rol: {0}" -f ($login.user.roles -join ", ")) -ForegroundColor White
$login2 = Probar 7 "Segundo login (otro dispositivo)" POST "/auth/login" @{ email = $correo; password = "Clave2026*" } 200 $null "Chrome"
$token2 = $login2.token

Write-Host "--- Rutas protegidas ---" -ForegroundColor Cyan
Probar 8 "Error: ver perfil sin token" GET "/auth/me" $null 401 | Out-Null
Probar 9 "Ver mi perfil con token" GET "/auth/me" $null 200 $token | Out-Null
Probar 10 "Editar mi perfil" PUT "/auth/me" @{ firstName = "Laura"; lastName = "Torres Vega"; phone = "0998887766" } 200 $token | Out-Null
$sesiones = Probar 11 "Ver mis sesiones" GET "/auth/sessions" $null 200 $token
Write-Host ("         sesiones activas: {0}" -f (@($sesiones | Where-Object { $_.isActive })).Count) -ForegroundColor White

Write-Host "--- Contraseña ---" -ForegroundColor Cyan
Probar 12 "Error: contraseña actual incorrecta" PUT "/auth/change-password" @{ currentPassword = "otra"; newPassword = "Nueva2026#" } 400 $token | Out-Null
Probar 13 "Cambiar contraseña (cierra las otras sesiones)" PUT "/auth/change-password" @{ currentPassword = "Clave2026*"; newPassword = "Nueva2026#" } 200 $token | Out-Null
Probar 14 "Error: el token del otro dispositivo ya no sirve" GET "/auth/me" $null 401 $token2 | Out-Null
Probar 15 "Error: login con la contraseña vieja" POST "/auth/login" @{ email = $correo; password = "Clave2026*" } 401 | Out-Null
$login3 = Probar 16 "Login con la contraseña nueva" POST "/auth/login" @{ email = $correo; password = "Nueva2026#" } 200 $null "Antigravity"
$token3 = $login3.token

Write-Host "--- Cerrar sesiones ---" -ForegroundColor Cyan
$sesiones = Probar 17 "Ver mis sesiones" GET "/auth/sessions" $null 200 $token
$otra = @($sesiones | Where-Object { $_.isActive -and -not $_.isCurrent })[0]
Probar 18 "Cerrar la sesión de otro dispositivo" DELETE "/auth/sessions/$($otra.id)" $null 204 $token | Out-Null
Probar 19 "Error: usar el token de esa sesión cerrada" GET "/auth/me" $null 401 $token3 | Out-Null
Probar 20 "Error: cerrar una sesión que no existe" DELETE "/auth/sessions/999999" $null 404 $token | Out-Null
Probar 21 "Cerrar mi sesión (logout)" DELETE "/auth/logout" $null 204 $token | Out-Null
Probar 22 "Error: usar el token después del logout" GET "/auth/me" $null 401 $token | Out-Null

Write-Host ""
Write-Host ("RESULTADO: {0} pruebas correctas, {1} con problemas" -f $ok, $fallos) -ForegroundColor $(if ($fallos -eq 0) { "Green" } else { "Red" })
Write-Host ""
