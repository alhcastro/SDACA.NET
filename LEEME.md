# CoopeNet

Migración del ERP SDACA de VB6 a **VB.NET**, con alcance en la licencia de **Coopepalmares** (cédula 3-004-045003, cliente 7, 18 módulos).

Estrategia: se migra módulo por módulo a un ejecutable .NET propio, `CoopeNet.Erp`, que tiene su entrada y su menú. Usa la misma base MySQL y los mismos `.ini` que VB6, así que los dos sistemas conviven: lo que falta migrar se sigue usando en SDACAERP. La versión anterior, que cargaba los módulos .NET dentro del menú VB6 por COM, se retiró; está en el historial hasta el commit `073ef32`.

Todo el código es VB.NET con `Option Strict On`, sobre .NET 8. La versión de .NET y las opciones comunes están en `Directory.Build.props`.

## Proyectos

| Proyecto | Destino | Para qué |
|---|---|---|
| `src/CoopeNet.Core` | net8.0 | Reglas sin interfaz: entrada y sesión, MD5 compatible con VB6, conexión MySQL, licencia, privilegios, parámetros, bitácora y repositorios |
| `src/CoopeNet.Ui` | net8.0-windows | Controles WinForms comunes (barra de comandos, buscador, mensajes) y el contrato `IModulo` |
| `src/CoopeNet.SD1101` | net8.0-windows | Bodegas de Herramientas, en migración |
| `src/CoopeNet.SD1501` | net8.0-windows | Emisión y Control de Cheques, en migración |
| `src/CoopeNet.Erp` | net8.0-windows | El ejecutable `CoopeNet.exe`: entrada, menú y cierre de sesión |
| `tests/CoopeNet.Core.Tests` | net8.0 | Pruebas xUnit en VB.NET. Algunas leen `C:\Sistemas\Fuentes` para comparar con el código VB6 |

## Comandos

```powershell
dotnet build -c Release
dotnet test -c Release
dotnet run --project src\CoopeNet.Erp -c Release
```

`CoopeNet.Erp` lee los `.ini` de `C:\Program Files (x86)\SDACA\Bin`. Se cambia con `-inipath <carpeta>`, igual que en SDACAERP.

## Agregar un módulo

1. Crear `src/CoopeNet.SDxxxx` con una clase `Modulo` que implemente `IModulo` (ver `SD1101\Modulo.vb`). Las opciones que falten se declaran con `OpcionMenu.Pendiente`.
2. Agregar la referencia en `CoopeNet.Erp.vbproj` y el módulo en `Programa.Modulos()`.

El menú muestra solo los módulos de la licencia y las opciones que el usuario puede abrir.

## Apariencia

`Tema.Aplicar` (`CoopeNet.Ui\Tema.vb`) viste cada pantalla al abrirla desde el menú: colores, encabezado, tarjetas, rejillas y escalado a la resolución del equipo. Un formulario nuevo no necesita código de diseño:

- Use nombres conocidos para los botones: `btnGuardar`, `btnImprimir`, `btnEntregar` y `btnAplicar` salen como acción principal; `btnAnular` y `btnBorrar`, como acción de riesgo.
- Use `CoopeNet.Ui.Pestanas` en lugar de `TabControl`.
- Tome los colores de `Tema`, no de `Color`. No fije `ForeColor` en los hijos de un grupo: `FrmVerCheque` pinta un cheque anulado por herencia.
- Declare el ícono de la tarjeta en `OpcionMenu.Formulario` con una constante de `IconoMenu`.
- Una pantalla con listas o muchos datos se puede agrandar y maximizar: ancle sus controles y llame `Ventanas.Recordar(Me)` en el constructor, antes de `Tema.Aplicar`. El tamaño de diseño queda como mínimo y al cerrar se guardan el tamaño (a 96 DPI) y si quedó maximizada en `%LOCALAPPDATA%\SDACAentanas.ini`; la posición no, para que siga abriendo sobre la pantalla que la llama. Los controles dentro de pestañas se anclan en `Load`, cuando las páginas ya tienen su tamaño (ver `FrmArticulos.AnclarPestanas`). Así están por ahora el Catálogo de Artículos, sus diálogos de proveedores, códigos de barras y alternos, y `FrmBuscador`.

El panel `vista` de `FrmConfigCheque` representa el papel impreso y queda fuera del tema.

`AgregarEncabezado` suma el alto del encabezado al área antes que al mínimo. Hasta el 2026-11-07 lo hacía al revés y, si el mínimo coincidía con el tamaño, el encabezado se sumaba dos veces: `FrmAnularCheque` y `FrmVerCheque` de SD1501 tenían 60 px de más al fondo, y ahora quedan con su alto de diseño.

## Reglas que hay que conservar mientras convivan VB6 y .NET

- **MD5:** `Md5Vb6.Calcular` usa el byte bajo de cada carácter y devuelve minúsculas, igual que `SD_CLS_MD5.NEWMD5`. Con él se calculan las contraseñas de MySQL y los hashes de `gatusuariospriv2`. No se debe cambiar a UTF-8.
- **Usuarios de MySQL:** `Administrador` entra como `root` con la contraseña tal cual. Los demás usuarios usan `MD5(contraseña)`. Sin usuario se entra como `sdaca`.
- **Entrada:** `ServicioEntrada` migra `SD_Procesar_Entrada`. Difiere de VB6 en dos puntos, explicados en el propio archivo: revisa horario y datos antes de registrar la sesión, y si el administrador responde No a entrar con sesión en otra estación, no entra.
- **Privilegios:** `Privilegios.CargarAsync` carga los permisos al iniciar sesión, igual que `ObtenerUsuario`. Administrador y root reciben todos los de los módulos del local. Los demás reciben solo los que tienen su hash en `gatusuariospriv2`. `Privilegios.Validar` funciona igual que `SD_ValidarPriv`. `TieneAsync` es la versión corregida de `TienePrivilegio`: el original de VB6 no filtraba por el privilegio pedido, aunque ninguna parte del código lo llamaba. Se corrigió también en `Comun\SD_MOD_USU.bas`.
- **Parámetros:** si el local no tiene el parámetro, se usa el del local `1000` (valores globales). `Vb6.Val` es `CLng(Val(...))` de Microsoft.VisualBasic, igual que `Get_Parametro_Lng`.

## Siguiente

`SD1501` (ECC, Emisión y Control de Cheques). Ya están las opciones propias del módulo: `FrmVerCheque`, `FrmEntrega`, `FrmConfigCheque`, `FrmAnularCheque`, `FrmMoveCheque`, `FrmPrintCheque`, `FrmPrintChequeM` y `FrmEditarCheque`. Faltan las que abren otros módulos (Bancos, Monedas, Cuentas Bancarias, Pago a Proveedores, Pago Recibos de Café, Retiros a la Vista, Tramitar Pagos), que remiten a SDACAERP.

**Pruebas:** se usa el catálogo `sdprincipal` del servidor de pruebas. Las tablas son MyISAM, así que una transacción no deshace nada: antes de escribir hay que respaldar las filas y restaurarlas al final. No crear `ccbcierre` ni agregar definiciones a `gatreplicadef` ahí; para eso se usa un catálogo aparte.

**Probado (2026-11-05):** Anular (con pago de CxP), Borrar y Mover (con recibo de CxP y de café) en `sdprincipal`, restaurado después. El caso `# Cheque Repetido` no se probó: ninguna de las 15 cuentas lo produce hoy. Grabar la configuración y entregar cheques, el mes cerrado y la replicación, en un catálogo desechable ya eliminado. Falta abrir SD1501 desde el menú real, con una entrada de verdad.

**`ccbcierre` no existe en ningún catálogo del servidor:** en VB6 ese error se pierde y todo mes cuenta como abierto, así que anular siempre borra el movimiento de `ccblibro`, aunque sea de un mes viejo. Los datos lo confirman: de 432 cheques anulados desde 2025, 431 ya no tienen movimiento. `LibroBancos.MesCerradoAsync` conserva ese resultado solo cuando falta la tabla.

**Replicación:** `BANCOS` no tiene destinos en `gatreplicadef`, pero `LibroBancos` pasa por `Replicador`, que migra `SD_Replicar2`. Lo demás que VB6 replica debe usarlo también.

**Diferencias con VB6, a propósito:**

- `FrmAnularCheque` actúa sobre el cheque cargado, no sobre el número escrito en la casilla.
- Un error en el libro de bancos se muestra. En VB6, `On Error Resume Next` lo ocultaba.
- `FrmConfigCheque` graba en el local del usuario cuando el banco no tiene configuración. VB6 grababa en el local anterior o en el 0.
- `FrmConfigCheque` aplica las medidas al salir de cada casilla, no con cada tecla.

**`ECCDiasBorrar`:** el 1000 de `SD_LoadFromParametros2` es el local global, no una cantidad de días. Sin el parámetro, que no existe en la base, Borrar solo aparece para cheques con fecha de hoy.

**Impresión de cheques:** `CHKPAPI` está en todos los locales, así que el cheque es texto para las Epson FX-890 (ESC/P), armado con la plantilla de `gatprintapi` por `PlantillaImpresion`, que migra `SDPrintInfoAPI2`. El reporte gráfico (`RptCheque`) no se migró, y `eccchequesconfig` no interviene al imprimir: `FrmConfigCheque` funciona, pero en este cliente no cambia el cheque impreso. `ImpresionMatricial` envía el texto crudo a la impresora o lo dibuja en PDF interpretando ESC/P.

**No migrado de `FrmPrintCheque`:** `INIT`, que usan CxP, Café y Retiros; las cuentas en otra moneda, que se rechazan con un aviso; los tipos de asiento con `sqltext`; y el comprobante (`ECCPrintComprobante`, sin definir). Hoy ninguna cuenta ni tipo los usa. `FrmPrintCheque2` es una versión vieja y más pequeña: no hay nada que recuperar de ahí.

**Probado (2026-11-05):** registrar un cheque en `sdprincipal`, restaurado después, y generar el PDF de un cheque real y del nuevo. Falta enviar a una Epson FX real y pulsar Imprimir en la pantalla.

**Editar Asiento:** difiere de VB6 para no perder asientos. Allá, si el tipo del cheque no está en `eccconta` para el local (los tipos 2 y 4 no lo están en el local 0), el combo queda vacío y el `INSERT` falla en silencio después del `DELETE`. Además, cargar el cheque dispara el evento del combo, que reemplaza las líneas en los tipos con cuentas fijas. Aquí se conserva el tipo del cheque y solo se reacciona si el usuario lo cambia. Igual que en VB6, se puede editar un cheque conciliado, se guarda aunque el asiento no coincida con el cheque (avisa) y las líneas vuelven a quedar sin contabilizar.

**Impresión Masiva:** solo lee. `frmgenerarasientos2`, que se abría con doble clic en la etiqueta Banco, no se migró: es una utilidad de un solo uso con el cheque 46008 fijo en el código.

**`SaveDContable` no coincide con la base:** en `Fuentes` inserta 15 valores en `eccchequesconta`, que tiene 14 columnas, y lee una columna 7 que la rejilla no tiene. Aun así los cheques impresos y movidos tienen asiento, así que lo que corre en producción no es esa línea. `MoverAsync` graba las 14 columnas reales y conserva `id` y `codigo`, que `Fuentes` no pasa. Impresión usa la misma rutina.

**Mover conserva dos riesgos de VB6:** si el número nuevo ya existe, avisa pero sigue y deja el cheque viejo anulado, sin cheque nuevo y con otro movimiento en el libro. Y el recibo de café se busca solo por número de cheque, sin banco ni cuenta.

**Fuentes:** aquí `Fuentes\SD1501` (`.vbp` 2011.158) es más nueva que el `SD1501.dll` instalado (junio 2010), que no trae `FrmEntrega` ni las opciones 016 y 017. `FrmPrintCheque2` salió del proyecto y su fuente sigue en `C:\Sistemas\Cuarentena6-11-03\SD1501`; hay que compararlo con `FrmPrintCheque` al migrar Impresión. `FrmReporte` está en `Comun` y `FrmEditar` es un marco dentro de `FrmEditarCheque`. Falta confirmar el DLL de producción.

**Entregar Cheques parece sin uso:** ningún cheque de `centro20261031` tiene `fecharetiro`. Enter con el filtro vacío trae los ~420 mil cheques.

`SD1101` (SBH, Sistema de Bodegas de Herramientas), en pausa. Ya están `FrmTiposPrestamos` y `FrmTiposHerramientas`. Las demás opciones muestran un aviso de que no están migradas y remiten a SDACAERP.

**Probado contra la base de pruebas (2026-11-05):** la entrada, los privilegios, el menú y el cierre de sesión funcionan por la interfaz con `Administrador`.

**SD1101 no se puede probar en esa base:** ningún local tiene el módulo 11 en `gatmoduloslocal`, las tablas `sbh*` no existen en ningún catálogo del servidor y `gatprivilegios` solo trae 11000 y 11001, mientras `Modulo.vb` usa de 11002 a 11007. Por eso el menú sale vacío y `FrmTiposHerramientas` y `FrmTiposPrestamos` siguen sin ejecutarse contra MySQL. Falta confirmar si Coopepalmares usa este módulo.

**Fuentes incompletas:** el `SD1101.dll` instalado (2010.155.629) es más nuevo que `Fuentes\SD1101` (2009.153.2014). Trae las opciones 016 y 017 y `frmQMovimientos`, `frmQTiposHerra`, `rptDevol`, `rptQMovimientos` y `rptQTipoHerra`. Solo hay un `frmQMovimientos` en `Fuentes\SD2701`, sin confirmar si es el mismo; los otros cuatro no aparecen en `C:\Sistemas`. Por eso no se debe confiar solo en `Fuentes`: hay que comparar cada módulo con su DLL instalado.

## SD0001 - Gestión de Asistencia Técnica

Está el menú de las opciones que atiende su `Accion`, con `FrmDatosPersonales`, `FrmAdminCodigos`, `FrmCambioId`, `FrmFallecido`, `FrmPadron`, `FrmAgenda`, `FrmRepararTablas`, `FrmConfiguracionImpresion`, `FrmBancos`, `FrmMonedas` y `FrmTipoCambio`; lo demás remite al sistema anterior. SD1401 y SD1501 abren Bancos y Monedas con su propio privilegio. Usuarios, departamentos y respaldos los atiende el menú de VB6, no este módulo.

**La fuente es más nueva que la base:** `gatbancos` no tiene siglas ni web services, `gatmonedas` tiene `Iniciales` y `gattipocambio` es por moneda y día, sin banco ni hora. Se siguió la base. `Numloc` está vacío en todos los bancos y no se migró ese control. Guardar un banco conserva `Finport`, que usa la conciliación CSV.

**Tipo de cambio:** `gattipocambio` no recibe datos desde 2008; el que se usa está en `postipocambio`, de POS.

**Datos Personales:** `FrmDatosPersonales` migra la lista, el buscador, Nuevo, Editar y las pestañas Ficha General, Ubicación y Contacto (`SDLoc` y `NumLoc`), Representante Legal y Códigos. `FrmCodigoPersona` migra `FrmAgregarTiposCodigos` y `FrmImagenes` migra `FrmImgsCliente`. `DatosPersonalesRepositorio` replica `gacdp` y `gacdprepresentante2` como `DP`, y `gaccodclientes` como `posclientes`. `gacdptel` y `gatimagenes` no se replican, igual que en VB6.

No migrado: `ClsDP`, la entrada desde otros módulos; el buscador del padrón (`LblPadron`), aunque sí se busca la cédula al digitarla; y la acción de SD1901, que es de otro cliente.

Sigue la base y no `Fuentes`: `nombre` admite 100 caracteres, los tipos de identificación salen de `gactiposid` (DIMEX, NITE y otros) y el largo se limita a los 16 de `gacdp.id`. Las 1,533 personas con `fnac` en cero se muestran como 1900-01-01, y al guardarlas queda esa fecha, igual que en VB6. Las 666 identificaciones que no cumplen el tamaño de su tipo siguen sin poder guardarse hasta corregirlas.

Difiere de VB6 en:

- **`numloc`:** no se vacía al guardar. VB6 lo borra, y 1,706 personas aún lo tienen.
- **Representantes:** se actualizan con `ON DUPLICATE KEY UPDATE`, solo en personas jurídicas. En VB6 el `COUNT` compara contra un texto fijo y siempre da 0, de modo que el `INSERT` choca con la llave y el `DELETE` de `orden` negativo puede borrar el representante. Los datos no lo confirman: hay 18 filas y ninguna con `orden` negativo.
- **Códigos:** solo se agregan o eliminan al editar una persona ya guardada, y se replican solo si la sentencia funcionó. Agregar no reemplaza el código seleccionado, igual que en VB6, por eso el botón dice Agregar código. Los códigos de empleado no se pueden eliminar aquí: en VB6 avisaba éxito sin borrar nada.
- **Fotografía:** se cambia al editar una persona ya guardada, para no dejar imágenes sin dueño.
- **Textos:** los apóstrofos se escapan. En VB6, `O'Brien` rompe la sentencia.
- **`uidc`:** los contactos y códigos graban el usuario real, no 0.

**Probado contra `sdprincipal` (2026-11-06), con identificaciones sintéticas y el local 7, que no tiene filas en `gatreplicadef`:** lecturas de 343 casos borde, y por la pantalla Nuevo, Editar, Guardar, Cancelar, la cascada de ubicación y los representantes. Todo se borró al final y los conteos quedaron iguales, sin réplicas encoladas. `DP` tiene destinos reales en los locales 0, 1, 4 y 9. Falta probar a 125 %, con un usuario sin privilegios y agregando una foto real por el diálogo de archivos.

**Administración de Códigos:** `FrmAdminCodigos` migra `FrmAdminCodigos` y `FrmTiposCodigo` migra `FrmMantCodigos`, que no tiene opción en el menú: se abre con el botón Tipos de `FrmCodigoPersona`, que reemplaza a `<<AGREGAR TIPOS DE CODIGOS>>` del combo, con el privilegio 12. `CodigosRepositorio` replica `gaccodclientes` como `DP`, no como `posclientes`: así lo hace VB6 en esta pantalla y cada identificador tiene destinos distintos. Los tipos se replican como `GATMANT`, y borrarlos no se replica, igual que en VB6.

No migrado: incluir a una persona que no existe. VB6 la agrega desde el padrón con un `INSERT ... SELECT` por posición, o abre `ClsDP`; aquí se avisa y se remite a Datos Personales.

**Cambio de Identificación:** `FrmCambioId` migra el asistente de cuatro pasos de `FrmCambioID` y `CambioIdentificacionRepositorio` migra `SD_CambiarID`: recorre `gatcambioid` y cambia cada columna con `UPDATE IGNORE`, replicado como `DP`. Se replica aunque la sentencia falle aquí, igual que en VB6, porque cada local tiene sus propias tablas. Lo ya cambiado no se deshace: las tablas son MyISAM. El modo de eventos no se migró porque solo lo usa `ClsDP`, y `FrmCambioIdMasa` tampoco: su opción está comentada en `Clase.cls`.

Difiere de VB6 en:

- **Resumen:** cuenta los éxitos. VB6 muestra los errores como si fueran cambios exitosos.
- **Identificación ya existente:** avisa antes de confirmar. Con `UPDATE IGNORE`, las filas que chocan con una llave se quedan con la identificación original, sin error.
- **Validaciones:** rechaza una identificación igual a la original y limita el largo a los 16 de `gacdp.id`.
- **Paso 4:** no deja volver. En VB6, Anterior y Finalizar repetían el cambio.
- **`gatcambioid` vacía:** avisa que no se cambió nada.

**Fallecidos:** `FrmFallecido` migra `FrmFallecido`. Registrar graba `fdef` y deja `activo` en 0; anular vuelve `fdef` a 1900-01-01 y deja `activo` en 1, aunque la persona estuviera bloqueada antes, igual que en VB6. Ambos se replican como `DP`. La casilla admite códigos con `CoincidenciasAsync`, igual que el `SDT` con `BuscarID`, y el buscador deja por fuera a las personas jurídicas, aunque se puede digitar su identificación.

Difiere de VB6 en:

- **Búsqueda:** el código exacto usa `gac_fun_asociados` con local 0, igual que VB6, y encuentra asociados inactivos. Después `SD_Obtener_ID` busca solo la clave exacta; aquí se buscan coincidencias parciales.
- **Detalle:** los contactos salen de `gacdptel` y `gacdpteltipos`, no de la vista `vtel`.
- **Códigos:** se listan también los de empleado, como en Datos Personales, y `uidc` graba el usuario real, no 0. Se replican solo si la sentencia funcionó. Un código que la persona ya tiene se rechaza, en lugar de avisar éxito sin hacer nada.
- **Tipos:** piden tipo y descripción, y borrar pide confirmación.

**Repositorios probados, pantallas sin probar:** `herramientas/VerificarBase` confirmó en el 3315 las columnas de `gactiposcodigos` y `gatcambioid`, y los repositorios de Códigos, Cambio de Identificación y Fallecidos se probaron ahí con datos sintéticos en el local 7. Cada cambio de identificación tardó unos 40 segundos. Falta la prueba de pantallas que describe el `LEEME.md` de esa herramienta. Si la identificación nueva ya existe, el resumen cuenta `gacdp` y también `dp`, que es una vista de `gacdp`. `gacdptel` tiene triggers que escriben en `gacdptel_bitac`: al borrar personas de prueba hay que limpiar también esa tabla.

**Padrón Nacional y Jurídico:** `FrmPadron` migra `FrmBuscaPadron` en el modo del menú, que solo consulta, y `PadronRepositorio` migra `SD_Padron` y `SD_PadronJ`. Como en VB6, la cédula manda sobre los demás campos y busca por el inicio, los apellidos son exactos, el nombre se busca en cualquier parte y se muestran hasta 500 filas, con un aviso si hay más. Difiere en que no consulta sin criterio (en VB6 la sentencia fallaba), usa parámetros en lugar de concatenar el texto, ignora los guiones de la cédula y avisa si el servidor no tiene la base `padron`. `Sexo` se muestra tal como está: `M`, `F` y unas pocas filas con `0` o `3`. Las 1,414 personas con `Nacimiento` en cero se muestran sin fecha. Falta el modo de selección que usa `LblPadron` en Datos Personales.

**Padrón probado en el 3315 (2026-11-07), solo lecturas:** `padron.padron` (unos 4 millones de filas) tiene índices por cédula, cada apellido, nombre y los tres juntos; `padron.padronj` (259,065) solo por cédula. Por cédula, apellidos o apellidos y nombre responde en menos de 0.2 s, y el jurídico por nombre en 0.1 s. Buscar solo por nombre no usa índice porque busca en cualquier parte: con un nombre común tarda unos 3 s, y con uno que no existe recorre todo el padrón y tarda unos 47 s, igual que en VB6.

**Agenda Telefónica:** `FrmAgenda` migra `FrmAgenda` y `AgendaRepositorio` migra `SD_Cargar_Lista` y `SD_Agenda_Telefonicai` de `ModPrin.bas`. Igual que VB6, lista hasta 100 personas activas y no fallecidas, con el código de asociado y el alias. Busca sola desde 3 letras, y cada espacio separa partes que deben aparecer en ese orden. La ficha se arma en HTML en un `WebBrowser`, con el menú de VB6: imprimir, vista previa, configurar página, copiar y exportar. El encabezado lleva el teléfono del local (`gacdptel` con id `local{n}` y tipo 0). Nuevo y Editar abren Datos Personales, que ahora acepta `IdInicial` para abrir en una persona; no entran directo a editar como en VB6, sino que la editan con su propio botón y sus privilegios. Los enlaces Nuevo, Modificar e Imprimir de la ficha pasan a botones. `FrmAgendaShow` no se migró: es la entrada desde otros módulos por `ClsDP`.

Difiere de VB6 en: busca con parámetros y escapa el HTML; la dirección omite el dato de ubicación que falta, en lugar de quedar en blanco (486 personas); los tipos 0 a 3 conservan el texto de VB6 y los demás salen de `gactiposid`, que en VB6 quedaban vacíos (165 con tipo 4). No toma en cuenta `GATDAE`, que en VB6 cambia el nombre del registro por el del local.

**Agenda probada en el 3315 (2026-11-07), solo lecturas:** cada búsqueda tarda medio segundo, porque recorre las 115,544 personas activas y vivas, igual que VB6. Las 100 fichas de una lista salieron completas, sin identificaciones repetidas. La pantalla se revisó con datos ficticios a la escala del equipo.

**Reparar BD:** `FrmRepararTablas` migra `frmRepararT` y `Core.Datos.ReparacionTablas` sus consultas. Lista las tablas del catálogo, filtradas por el inicio del nombre, y tiene los dos modos de VB6. Reparar corre `REPAIR NO_WRITE_TO_BINLOG TABLE ... QUICK EXTENDED USE_FRM` en cada tabla marcada y muestra lo que responde MySQL. MyIsamCHK arma una línea de `myisamchk` por tabla, que se puede corregir a mano, y la corre en este equipo: solo sirve en el servidor y con MySQL detenido. Difiere de VB6 en que pide confirmación antes de empezar, muestra el motor de las tablas que no son MyISAM (`REPAIR` no sirve en InnoDB ni en vistas), propone la carpeta de datos del servidor (`@@datadir` y el catálogo), pone comillas a las rutas, toma `_` y `%` del filtro como texto y muestra lo que escribe `myisamchk`.

**Reparar BD probado en el 3315 (2026-11-07):** lista las 1,306 tablas (874 MyISAM, 388 InnoDB y 44 vistas). Se reparó una tabla MyISAM sintética creada para la prueba: respondió `OK` con el aviso `Number of rows changed from 0 to 4`, que es normal con `USE_FRM` porque rehace el índice, y los datos y el `AUTO_INCREMENT` quedaron intactos. En InnoDB MySQL responde que no lo admite, y en una tabla inexistente devuelve el error como fila, sin excepción. Las tablas de prueba se borraron. No se corrió `myisamchk`: el servidor está en otro equipo.

**Sincronizar no se migra:** `frmsync` copia tablas entre esta base y un servidor remoto (parámetros `AgServer`, `AgDB`, `AgPuerto` y `AgPass`) según la tabla `sync`, y menciona el catálogo `gyq` de otro cliente. En el 3315 no existen ni la tabla ni los parámetros, y el usuario confirmó el 2026-11-07 que Coopepalmares no la usa. Se quitó del menú.

**Configuración de impresión y consecutivos:** `FrmConfiguracionImpresion` migra `Comun\FrmManImp.frm` (opción 048) y `Core.Impresion.ConfiguracionImpresion` sus sentencias sobre `gatimpresion`. Igual que VB6, se elige el documento (los de la tabla más los seis fijos de `Load_Tipos_Documentos`), se editan departamento, estación, impresora y consecutivo; Borrar marca la fila y Aplicar graba todo, sin replicar. La impresora se escribe libre, con sugerencias de las de este equipo y las ya configuradas.

Difiere de VB6 en:

- **Consecutivo:** se ve y se edita en todos los documentos. VB6 solo lo mostraba en `CHEQUES`, pero en el 3315 lo usan `CXCRECIBO`, `CXCRECIBOI`, `CXPRECIBO` y la cuenta de cheques `CHK2785`, que `ConsecutivoCheques` lee.
- **Guardar:** el `UPDATE` graba la fila completa con la llave original. En VB6 cada campo cambiado pisaba al anterior en el `SET`, y con dos cambios la sentencia quedaba como `SET , impresora = ...` y fallaba.
- **Validación:** avisa las filas repetidas antes de grabar, informa las que MySQL rechaza, borra antes de insertar para poder reusar una llave y pregunta antes de descartar cambios. Usa parámetros en lugar de concatenar.
- **Códigos que ya no existen:** un departamento o una estación que está en `gatimpresion` pero no en su catálogo se muestra como `(NO EXISTE)` en lugar de perderse (hay una estación así en el 3315).

**Probado en el 3315 (2026-11-07):** se leyeron las 59 filas de los 23 documentos. Con un documento sintético `ZZQ PRUEBA` se insertó, se cambiaron departamento, impresora y consecutivo a la vez, se rechazó una llave repetida y se borró e insertó con la misma llave; al final `gatimpresion` quedó idéntica (misma huella MD5). La pantalla se revisó a la escala del equipo sin aplicar cambios.

**SD0001 queda completo** para Coopepalmares: solo faltan `ClsDP` y `FrmAgendaShow` (la entrada desde otros módulos), el modo de selección del padrón y `FrmCambioIdMasa`, cuya opción está comentada en VB6.

## SD0502 - Facturación e Inventarios (POS)

Proyecto `CoopeNet.SD0502`, migrado de `Fuentes\SD0502(Coopepalmares)`, que según el usuario es el único módulo cuyas fuentes están al día. Por ahora solo tiene el Catálogo de Artículos; las demás opciones del `Menu_Order` (unas 80) se agregan al migrarlas. SD0501 es el POS genérico de otros clientes y no se migra.

**Catálogo de Artículos (opción 003, privilegio 5009):** `FrmArticulos` migra `frmMantArticulos`, `Core.Pos.ArticulosRepositorio` sus consultas y `Core.Pos.PreciosArticulo` el cálculo de `SD_ConsPrecImp`, `SD_DevUtilidad` y `SD_Rnd`. Tiene la ficha con las pestañas Detalle, Precio e Impuestos y otros, la navegación por código (filtrada por categorías), la búsqueda por código de barras o código corto (con las etiquetas de balanza 26 y 28), el buscador, y Nuevo, Editar y Borrar con el privilegio 5037. Utilidades, precios, extras, flete y descuentos piden el 5056. Cambiar una utilidad o un extra recalcula el precio, y cambiar el precio recalcula la utilidad. Al guardar replica `posarticulos` como `ARTICULOS` y el alta del local (`posarticuloslocal` y `posinventario`) como `posarticuloslocal`, y anota en `posvitmovimientos` los campos y las utilidades que cambiaron. Antes de guardar avisa si otro usuario cambió el artículo, como `SD_CompReg`.

**Proveedores:** el botón Proveedores de la pestaña Detalle abre `FrmProveedoresArticulo`, que migra el marco que VB6 mostraba en lugar de las pestañas al pulsar Agregar, con `Core.Pos.ProveedoresArticuloRepositorio`. Muestra las filas de `poscodprove` del artículo en el local y permite agregar (identificación con Enter, o la lista de proveedores activos del local; cuenta; código del proveedor; cantidad por empaque) y quitar (tecla Supr o botón Quitar). Listo borra y vuelve a insertar las filas del artículo en el local y replica cada sentencia como `posarticuloslocal`, con `$Prefijolocal` delante de la cuenta (R, E o Z en los locales 2, 3 y 9) salvo que ya lo tenga, y con la pausa de un segundo de VB6 entre el borrado y las altas, porque `gatreplica` se procesa por `Fecha`. Cancelar descarta. Difiere de VB6 en: solo se abre al editar un artículo existente, como las imágenes; una cuenta vacía queda vacía (VB6 le ponía el prefijo y en los locales 2, 3 y 9 quedaba `R`, `E` o `Z`); rechaza una fila repetida y una cantidad por empaque que no sea mayor que cero; y la identificación se busca exacta (identificación, código de asociado o de cliente único) y si no se encuentra abre la lista de proveedores, sin el buscador de coincidencias parciales de `SD_Obtener_ID`.

**Códigos de barras:** el botón Códigos de barras de la pestaña Detalle abre `FrmCodigosBarras`, que migra `frmCodBarra` con `Core.Pos.CodigosBarrasRepositorio`. Propone el próximo código corto sugerido y muestra los códigos de `poscodart_codbarra` del artículo en el local. Como en VB6, cada código se graba al pulsar Enter y se replica como `poscodart_codbarra`; quitar uno se graba al aceptar o con el siguiente Enter, borrando y volviendo a grabar la lista con la pausa de un segundo. Difiere de VB6 en: solo se abre al editar un artículo existente; un Enter sin códigos quitados graba solo el código nuevo (VB6 borraba y volvía a grabar todos, unas 300 filas de `gatreplica` por lectura en un artículo con 104 códigos, con el mismo resultado); el código usado por otro artículo se rechaza antes de agregarlo a la lista (VB6 lo dejaba en la lista sin grabarlo); un código repetido cambia sus unidades en lugar de duplicar la fila; y las unidades deben ser mayores que cero (VB6 grababa 1). No migra el lector serial (`posscanestac`) ni el `ALTER TABLE` y el `UPDATE ... SET Local` que VB6 corría al abrir; en el 3315 solo 2 filas tienen `Local` 0. Un trigger copia cada alta en `poscodart_codbarra_log`.

**Alternos:** el botón Asignar alternos de la pestaña Impuestos y otros abre `FrmAlternosArticulo`, que migra `frmArtAlternos` con `Core.Pos.AlternosArticuloRepositorio`. VB6 mostraba un alterno a la vez con anterior y siguiente, que comparaban el código como número y ordenaban por el artículo en lugar del alterno; aquí se ven todos en una lista. Nuevo pide el código (Enter vacío abre el buscador de artículos del local), que debe ser un artículo registrado en el local, como `SD_CargarArt`; seleccionar uno permite cambiar su detalle (en mayúsculas, conserva la fecha de creación) o eliminarlo. Guardar es un `REPLACE` replicado como `posarticulosalternos`, y eliminar también se replica, cosa que VB6 no hacía. Además rechaza que un artículo sea alterno de sí mismo. En el 3315 la tabla está vacía y no tiene destinos en `gatreplicadef`. Se abre al editar un artículo existente, como los demás botones.

**Imágenes:** la pestaña Imágenes migra la de VB6 con `Core.Pos.ImagenesArticuloRepositorio`: muestra las de `posimagartic` con anterior y siguiente, y al editar permite Nueva (solo .jpg de menos de 256 KB), Guardar, Modificar la descripción y Eliminar. Como en VB6, cada botón graba en el momento, no al guardar el artículo, no replica y `Local` queda en 0, igual que las 11,412 filas del 3315. Difiere en que solo se habilita al editar un artículo existente (en un Nuevo VB6 dejaba grabar imágenes de un código que podía no guardarse), la primera imagen es la de menor `Consec` (VB6 no ordenaba) y la descripción se recorta a los 128 caracteres de la columna. `frmMantCategorias` guarda también en `posimagartic`, con `id` igual a la clave de la categoría, así que un artículo con el mismo número puede mostrar esa imagen, igual que en VB6.

Varias columnas de `posarticulos` se usan para otra cosa que su nombre: `CodAlterno` es el CABYS, `Grupo` la presentación, `SubGrupo` la canasta básica, `CodArtProv` la marca, `Recomendada` el monto de otros impuestos, `Exento` el porcentaje de IVA, `Porcentaje` el descuento máximo, `Gasto` permite negativos y `Consumo` es No facturar. Los precios se guardan sin IVA. `Moneda` guarda el símbolo.

Difiere de VB6 en:

- **Categorías:** se exigen al guardar, como dice el mensaje. En VB6 una línea posterior pisaba la validación y solo quedaba la de unidad. Todos los artículos del local 1 tienen categoría, así que no bloquea ediciones.
- **Buscador:** filtra por categorías. En VB6 el filtro quedaba dentro del `LEFT JOIN` de los códigos de barras y no filtraba.
- **Precio a utilidad:** usa el mismo IVA con que se muestra el precio. VB6 siempre quitaba el IVA, aunque la casilla estuviera desmarcada. Con costo 0 la utilidad queda en 0, donde VB6 mostraba un error.
- **Código nuevo:** se calcula como `pos_fun_obt_codigo2` (el primer hueco desde 10000) sin crear la tabla `postmpcodigos`, que VB6 creaba con el mismo nombre para todos los usuarios.
- **Bitácora de utilidades:** se anota al guardar. VB6 la anotaba al salir de cada campo, aunque no se guardara.
- **Altas en el local:** usan la lista de columnas en lugar de `VALUES` por posición. Las sentencias van parametrizadas o escapadas.
- **Cancelar un Nuevo:** vuelve al artículo que se veía.

Igual que VB6: el costo no se edita aquí (lo fijan las compras), borrar no replica y deja `posarticuloslocal` y `posinventario`, y las fórmulas `G_ORDCOMPVAR` no se usan, porque solo las llenan Compras y Órdenes. `SD_Rnd` se portó con punto decimal: las utilidades guardadas son múltiplos de 0.005 en 82,169 de 82,174 artículos. El precio recalculado desde la utilidad coincide al centavo con 23,858 de 79,106 artículos, más que cualquier variante de la fórmula; los demás tuvieron cambios de costo posteriores en otras pantallas.

No migrado todavía: catálogo CABYS, etiquetas, sincronizar con el centro, existencias por local, movimientos y utilidades históricas (DLL `REP`), y el lector de código serial. La pestaña Lista no se migra: en VB6 nunca se llenaba. El cambio de código tampoco: su botón tiene `Visible = False` y la línea que lo mostraba está comentada, y actualizaba 29 tablas sin replicar. `SDInventarioHistorico`, que VB6 corre al abrir, tampoco.

**Probado en el 3315 (2026-11-07):** lecturas en el local 1 (catálogos, categorías, carga, navegación, buscador de 31,929 artículos en 1.8 s, códigos de barras, código nuevo y validación de borrado) y escrituras con el artículo sintético `ZZQ9001` en el local 7, que no replica: alta, edición, aviso de cambio de otro usuario, bitácora, borrado y limpieza, sin nada en `gatreplica`. Las imágenes se probaron igual con `ZZQ9001` (alta, consecutivos, reemplazo, descripción, borrado y limpieza) y 306 imágenes guardadas, incluidas las 6 PNG, se muestran bien. Los proveedores se probaron con `ZZQ9001` en el local 7 (alta con comilla en el código y cuenta vacía, quitar una, quitar todas), sin tocar el local 1 ni `gatreplica`; en el local 1 se abrió el diálogo al editar, se buscó un proveedor con dos cuentas, se agregó una fila y se canceló sin cambios en la base. Los códigos de barras se probaron con `ZZQ9001` en el local 7 (alta con comilla, unidades decimales, repetir un código, quitar uno, quitar todos), limpiando también `poscodart_codbarra_log`, sin tocar el local 1 ni `gatreplica`; en el local 1 se abrió el diálogo, se quitó un código y se canceló sin cambios. Los alternos se probaron con `ZZQ9001` en el local 7 (alta con comilla, cambio de detalle conservando la fecha, borrado) y la tabla quedó vacía. La pantalla se manejó en el local 1 sin guardar (cargar, navegar, editar y cancelar, nuevo y cancelar) y el artículo quedó igual. sdprincipal es la base del local 1: unidades, marcas y categorías solo existen ahí, y `ARTICULOS` se replica a los locales 1, 2, 3 y 9. No probar escrituras en el local 1.

## SD1401 - Control de Cuentas Bancarias

Todas las opciones están migradas. Bancos y Monedas son de SD0001 y se abren desde aquí. `FrmCuentasBancarias` y sus subpantallas también las abre SD1501. La opción 016 ("Pruebas no usar") no se migra; las 008 y 010 están comentadas en `Menu_Order`.

Antes de seguir hay que comprobar contra la base:

- **`ccbcierre`:** no existía en ningún catálogo. Se creó en `sdprincipal` de pruebas; para cualquier otro catálogo hay que aplicar `sql\2026-11-06-ccbcierre.sql`. Vacía no cambia nada. Al cerrar un mes, anular en él genera una reversión en lugar de borrar, también en VB6.
- **Saldos por Cuenta:** `MesInicio` sale de `sgcempresas.InitPer` (0 o sin empresa = 10). VB6 arma el fin de periodo con `MesInicio + 11` y día 30, que omite el día 31 y con mes 10 da `2025-21-30`; aquí el periodo son 12 meses exactos. Saldo Actual sin movimientos en el mes volvía al monto inicial de la cuenta; aquí parte del último cierre, como `CCBLoadMovimientos`, que VB6 ya no llama. Débito son las entradas (`tipo = 1`) y Crédito las salidas, también como `FrmLibro`: en VB6 solo están cruzados los alias del SQL. Se conserva que un mes sin movimientos salga en cero. Probado contra `sdprincipal` (2026-11-06), solo lectura: `InitPer` es 10 en todas las empresas y, en MySQL 5.6.40, la consulta de VB6 con esa fecha inválida no trae ningún movimiento. Las sumas por periodo coinciden con `ccblibro` y Saldo Actual con la fórmula de `CCBLoadMovimientos`.
- **`ccbcierrediario` en la base de pruebas:** en las cuentas 4796 y 207 del banco 1 el cierre no coincide con `ccblibro`, y el saldo final de un mes no empata con el inicial del siguiente. El puerto 3315 no está totalmente al día con el 3312, así que no indica un problema en producción ni en la migración.
- **Movimientos Bancarios (`FrmLibro`):** migra datos del movimiento, conciliación, buscar, guardar y borrar. No migra el Registro Contable: `ccbconta` tiene 3,109 filas casi todas de 2009 y en cero, porque `ECCMontoCheque` sale si la cuenta no trae cuatro datos; al editar se conservan las que haya. Tampoco los conceptos (`ccbconceptos` no existe), el tipo de cambio (termina en 2008) ni `Init`, la entrada desde otros módulos. `ComprobanteImpreso` reemplaza a `RPT0010` con vista previa; falta enviarlo a una impresora real. `gatimpresion` no tiene consecutivos `CCBTMOV`, así que el documento se digita. Difiere de VB6 en tres cosas: Saldo Actual es el real, Buscar pide una fecha desde (una cuenta llega a 319 mil movimientos) y Borrar revierte en mes cerrado, replica y rehace los cierres.
- **`CierresDiarios`:** migra `CCB_Cierre` con dos consultas en lugar de cinco por día. Recalculado en memoria, coincide en los 3,627 días de la cuenta 1253 desde 2015, con los tres montos. `FrmCalcularCierres` lo usa, y el aviso final solo sale si no hubo error.
- **Transferencias (`.ENV`):** son los pagos de Café y Nómina al Banco Nacional, y están en uso. `ArchivoTransferencia` migra el formato de `ccbtransferenciasconfig`: registros de 68 caracteres, ANSI y CRLF. **Falta compararlo byte a byte con un `.ENV` real antes de enviar uno al banco:** en este equipo no hay ninguno, y las pruebas salen de leer el código de VB6. Difiere de VB6 en que no genera sin formato o sin líneas, avisa antes de reemplazar un movimiento ya generado (regenerar borra el movimiento del libro, aunque esté conciliado) y rehace los cierres. Al guardar reemplaza todas las líneas: en VB6, cambiar el ID o el comprobante deja la línea vieja y se pagaría dos veces. `faplicada` nunca se llena, así que todas siguen como pendientes.
- **Conciliación:** `FrmConciliarArchivo` hace los pasos 1.a (CSV) y 1.b (TEC), `FrmMovimientosPendientes` los pasos 2, 3 y 4, y `FrmInformeConciliacion` el 5, con `InformeConciliacionImpreso` en lugar de `RPT0004` y `RPT0005`. El Banco Nacional usa TEC y los otros bancos CSV. **Falta probar con un archivo TEC y un CSV reales:** en este equipo no hay ninguno. El informe coincide con `ccbcierrediario` en 8 de 9 cuentas y meses probados; el noveno es la cuenta 4796 en setiembre, desfasada en el 3315. Difiere de VB6 en que rehace los cierres, avisa los errores y bloquea un mes cerrado también en CSV. El paso 3 de `Fuentes` manda el tipo dos veces y corre las columnas, de modo que el monto queda en 0 o 1: aquí sigue la estructura de `ccblibrobanco`. Se conserva el `tipomov` invertido del CSV, porque los datos de `ccblibrobancomal` lo confirman, y `ccblibrobancort` guarda las transferencias entre cuentas (`RECIBE DE`, `TRANSF. A`), que no se concilian.
- **`Init` de VB6:** altera `ccbcuentas` y `ccbcuentasconta` si falta `cconvh`. La base real no tiene esa columna: conserva `cconv` y `cconvd`, así que `Fuentes\SD1401` es más nueva que la base y .NET debe seguir el esquema real.

## Alcance: licencia de Coopepalmares

18 módulos (SD0401 está en reg.cls, pero Coopepalmares no lo usa), más el menú y las bases: unas **372 mil líneas**. Quedan fuera las variantes SD0507/SD0509 y los módulos de otros clientes (SD1901, SD20xx a SD27xx). La factura electrónica (`EnvioFC`) sí entra, porque tiene la cédula de Coopepalmares escrita en el código.
