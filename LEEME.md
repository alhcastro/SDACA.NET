# SDACA.NET

Migración del ERP SDACA de VB6 a **VB.NET**, con alcance en la licencia de **Coopepalmares** (cédula 3-004-045003, cliente 7, 18 módulos).

Estrategia: ruta B. Se migra módulo por módulo. El menú VB6 sigue funcionando y carga los módulos nuevos por COM. Al final se reemplaza el menú, y la lógica de negocio queda en una API ASP.NET Core compartida.

Todo el código es VB.NET con `Option Strict On`. La API ASP.NET Core también se escribe en VB.NET (controladores o API mínima). Lo único de ASP.NET Core que no admite VB.NET son las vistas Razor y Blazor; si algún día hace falta una interfaz web, esa capa iría aparte.

## Proyectos

| Proyecto | Destino | Para qué |
|---|---|---|
| `src/SDACA.Core` | net48 + net8.0 | Base común: contrato de `Init()`, MD5 compatible con VB6, conexión MySQL, licencia, privilegios, parámetros y bitácora |
| `src/SDACA.Com.SD1701` | net48 x86 | Primer módulo migrado, que el menú VB6 carga por COM |
| `tests/SDACA.Core.Tests` | net8.0 | Pruebas xUnit en VB.NET. Algunas leen `C:\Sistemas\Fuentes` para comparar con el código VB6 |

## Comandos

```powershell
dotnet build -c Release
dotnet test -c Release
```

## Reglas que hay que conservar mientras convivan VB6 y .NET

- **MD5:** `Md5Vb6.Calcular` usa el byte bajo de cada carácter y devuelve minúsculas, igual que `SD_CLS_MD5.NEWMD5`. Con él se calculan las contraseñas de MySQL, la clave de `Init()` (`MD5("SD0601")`) y los hashes de `gatusuariospriv2`. No se debe cambiar a UTF-8.
- **Usuarios de MySQL:** `Administrador` entra como `root` con la contraseña tal cual. Los demás usuarios usan `MD5(contraseña)`. Sin usuario se entra como `sdaca`.
- **Privilegios:** `Privilegios.CargarAsync` carga los permisos al iniciar sesión, igual que `ObtenerUsuario`. Administrador y root reciben todos los de los módulos del local. Los demás reciben solo los que tienen su hash en `gatusuariospriv2`. `Privilegios.Validar` funciona igual que `SD_ValidarPriv`. `TieneAsync` es la versión corregida de `TienePrivilegio`: el original de VB6 no filtraba por el privilegio pedido, aunque ninguna parte del código lo llamaba. Se corrigió también en `Comun\SD_MOD_USU.bas`.
- **Parámetros:** si el local no tiene el parámetro, se usa el del local `1000` (valores globales). `Vb6.Val` es `CLng(Val(...))` de Microsoft.VisualBasic, igual que `Get_Parametro_Lng`.
- **SDACA.Core apunta a net48 y net8.0**, no a netstandard2.0, para tener Microsoft.VisualBasic completo (Val, Mid, Format...) en los dos destinos.

## Fase 2: puente COM (módulos .NET dentro del menú VB6)

`src/SDACA.Com.SD1701` es el primer módulo migrado. Es VB.NET sobre .NET Framework 4.8 x86, con ProgID `SD1701.Clase`, y el menú VB6 actual lo carga con `CreateObject` sin cambios.

Cómo funciona:
- `Contrato.vb` define `IModuloSdaca` (Version, Init, SDClase, Menu, Accion, Menu_Order, SD_LoadRes, Preferencias) y los UDT `SD_Clase` y `SD_InfoMenu`, con los mismos nombres de campo que VB6. El menú los lee por nombre.
- `Init` recibe los UDT de `SDBASE00` (`lib/interop/Interop.SDBASE00.dll`, generado con TlbImp desde `C:\Program Files (x86)\SDACA\Bin\SDBASE00.dll`).
- `ModuloComBase.vb` implementa `IModuloSdaca` con lo que repiten todos los Clase.cls: la clave de Init, la copia del contexto y SD_LoadRes, que escribe el icono igual que VB6, con el relleno a 4 bytes. Cada módulo hereda de esta clase y completa `SDClase`, `Menu`, `Accion` y `Menu_Order` (ver `SD1701\Clase.vb`).
- Los iconos salen del `.RES` de VB6 con `herramientas/ExtraerRes.ps1`.

### Registrar, probar y revertir (sin administrador)

```powershell
# Compilar y publicar
dotnet build src\SDACA.Com.SD1701 -c Release
Copy-Item src\SDACA.Com.SD1701\bin\Release\net48\* publicado\SD1701 -Force

# Registrar solo para el usuario actual (HKCU). El menú de ese usuario carga la versión .NET;
# los demás usuarios siguen con VB6 (HKLM no se toca).
herramientas\Registrar-ComUsuario.ps1 -Carpeta publicado\SD1701 -Dll SDACA.Com.SD1701.dll

# Comparar VB6 contra .NET con el probador VB6 (pruebas-com/Probador). Deja registrada la versión .NET.
herramientas\Probar-Paridad.ps1 -Modulo SD1701

# Volver a VB6
herramientas\Registrar-ComUsuario.ps1 -Carpeta publicado\SD1701 -Dll SDACA.Com.SD1701.dll -Desregistrar
```

Resultado del 2026-11-03, ya en VB.NET: **PARIDAD OK**. 24 respuestas idénticas y el icono idéntico byte por byte.

### Siguiente

`SD1101` (CEV, Control de Equipos y Vehículos): 15 formularios y 5 reportes. Prueba que los formularios WinForms abran desde `Accion()` y que los reportes se puedan migrar.

## Alcance: licencia de Coopepalmares

18 módulos (SD0401 está en reg.cls, pero Coopepalmares no lo usa), más el menú y las bases: unas **372 mil líneas**. Quedan fuera las variantes SD0507/SD0509 y los módulos de otros clientes (SD1901, SD20xx a SD27xx). La factura electrónica (`EnvioFC`) sí entra, porque tiene la cédula de Coopepalmares escrita en el código.
