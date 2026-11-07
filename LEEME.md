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

El panel `vista` de `FrmConfigCheque` representa el papel impreso y queda fuera del tema.

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

Está el menú de las opciones que atiende su `Accion`, con `FrmDatosPersonales`, `FrmAdminCodigos`, `FrmCambioId`, `FrmFallecido`, `FrmPadron`, `FrmBancos`, `FrmMonedas` y `FrmTipoCambio`; lo demás remite al sistema anterior. SD1401 y SD1501 abren Bancos y Monedas con su propio privilegio. Usuarios, departamentos y respaldos los atiende el menú de VB6, no este módulo.

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
