# SDACA.NET

Migración del ERP SDACA de VB6 a .NET, con alcance en la licencia de **Coopepalmares** (cédula 3-004-045003, cliente 7, 19 módulos).

Estrategia: ruta B. Se migra módulo por módulo. El menú VB6 sigue funcionando y carga los módulos nuevos por COM. Al final se reemplaza el menú, y la lógica de negocio queda en una API ASP.NET Core compartida.

## Proyectos

| Proyecto | Destino | Para qué |
|---|---|---|
| `src/SDACA.Core` | netstandard2.0 + net8.0 | Base común: contrato de `Init()`, MD5 compatible con VB6, conexión MySQL, licencia, privilegios, parámetros y bitácora |
| `tests/SDACA.Core.Tests` | net8.0 | Pruebas. Algunas leen `C:\Sistemas\Fuentes` para comparar con el código VB6 |

## Comandos

```powershell
dotnet build -c Release
dotnet test -c Release
```

## Reglas que hay que conservar mientras convivan VB6 y .NET

- **MD5:** `Md5Vb6.Calcular` usa el byte bajo de cada carácter y devuelve minúsculas, igual que `SD_CLS_MD5.NEWMD5`. Con él se calculan las contraseñas de MySQL, la clave de `Init()` (`MD5("SD0601")`) y los hashes de `gatusuariospriv2`. No se debe cambiar a UTF-8.
- **Usuarios de MySQL:** `Administrador` entra como `root` con la contraseña tal cual. Los demás usuarios usan `MD5(contraseña)`. Sin usuario se entra como `sdaca`.
- **Privilegios:** `Privilegios.CargarAsync` carga los permisos al iniciar sesión, igual que `ObtenerUsuario`. Administrador y root reciben todos los de los módulos del local. Los demás reciben solo los que tienen su hash en `gatusuariospriv2`. `Privilegios.Validar` funciona igual que `SD_ValidarPriv`. `TieneAsync` es la versión corregida de `TienePrivilegio`: el original de VB6 no filtraba por el privilegio pedido, aunque ninguna parte del código lo llamaba. Se corrigió también en `Comun\SD_MOD_USU.bas`.
- **Parámetros:** si el local no tiene el parámetro, se usa el del local `1000` (valores globales).

## Siguiente fase

Fase 2, puente COM: un `SD1701.Clase` hecho en .NET Framework 4.8 x86 que el menú VB6 actual pueda cargar con `CreateObject`. Tiene que recibir los UDT `SDACA_*` de la typelib de `SDBASE00.dll`. Después sigue `SD1101`, para probar formularios y reportes.

## Alcance: licencia de Coopepalmares

18 módulos (SD0401 está en reg.cls, pero Coopepalmares no lo usa), más el menú y las bases: unas **372 mil líneas**. Quedan fuera las variantes SD0507/SD0509 y los módulos de otros clientes (SD1901, SD20xx a SD27xx). La factura electrónica (`EnvioFC`) sí entra, porque tiene la cédula de Coopepalmares escrita en el código.
