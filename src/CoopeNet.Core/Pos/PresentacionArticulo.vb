Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Lo que muestra y edita el Verificador de Presentación.</summary>
    Public NotInheritable Class PresentacionArticulo
        Public Property CodigoArt As String = ""
        Public Property DescCorta As String = ""
        Public Property Descripcion As String = ""
        Public Property UnidadMedida As String = ""
        ''' <summary>posarticulos.Grupo: el contenido del empaque (900 ml, 776 hojas...).</summary>
        Public Property Grupo As Double
        ''' <summary>Porcentaje de IVA.</summary>
        Public Property Exento As Double
        Public Property Precio1 As Double
        ''' <summary>Abreviatura de la unidad de medida (ML, GR...), o vacío.</summary>
        Public Property Abrevia As String = ""
    End Class

    ''' <summary>
    ''' Verificador de Presentación de SD0502 (Mantenimiento, opción 078, privilegio 5057). Migración de
    ''' SD0502\frmMantArticulos2.frm: SD_CargaGrup, SD_CargarArt, SD_GuardarArticulo y la búsqueda. El
    ''' anterior, el siguiente y el código de barras son los del Catálogo (<see cref="ArticulosRepositorio"/>).
    ''' </summary>
    Public NotInheritable Class PresentacionesRepositorio

        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(If(texto, ""))}'"
        End Function

        ''' <summary>SD_CargaGrup: las unidades del local.</summary>
        Public Async Function UnidadesAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionArticulo))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of OpcionArticulo)(New CommandDefinition(
                    "SELECT CAST(unidad AS CHAR) AS Codigo, CONCAT(unidad, ' - ', Descripcion, ' (', abrevia, ')') AS Descripcion FROM gatunidades WHERE local = @local ORDER BY unidad",
                    New With {local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>gFactRedond1 y gFactRedond1Dir de Clase.cls: POSFacRedon y POSFacRedonDir del local.</summary>
        Public Async Function RedondeoAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of (Factor As Double, Direccion As DireccionRedondeo))
            Dim parametros As New Parametros(_fabrica)
            Dim factor = Conversion.Val(Await parametros.ObtenerAsync(local, "POSFacRedon", ct).ConfigureAwait(False))
            Dim direccion = CInt(Vb6.Val(Await parametros.ObtenerAsync(local, "POSFacRedonDir", ct).ConfigureAwait(False)))
            Return (factor, If([Enum].IsDefined(GetType(DireccionRedondeo), direccion), CType(direccion, DireccionRedondeo), DireccionRedondeo.Ascendente))
        End Function

        ''' <summary>
        ''' SD_CargarArt. VB6 une gatunidades sin el local y toma cualquier abreviatura; aquí se prefiere la del
        ''' local. Nothing si el artículo no existe.
        ''' </summary>
        Public Async Function ObtenerAsync(codigo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of PresentacionArticulo)
            Const sql As String =
                "SELECT A.CodigoArt, A.DescCorta, A.Descripcion, A.UnidadMedida, A.Grupo, A.Exento, A.Precio1, " &
                "IFNULL((SELECT u.abrevia FROM gatunidades u WHERE u.unidad = A.UnidadMedida ORDER BY u.local <> @local LIMIT 1), '') AS Abrevia " &
                "FROM posarticulos A WHERE A.CodigoArt = @codigo LIMIT 1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of PresentacionArticulo)(New CommandDefinition(sql, New With {codigo, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>El Buscar de la barra. La segunda columna es el código.</summary>
        Friend Const SqlBuscar As String =
            "SELECT A.Descripcion AS DESCRIPCION, A.codigoart AS CODIGO, L.STOCK, A.preciocosto AS PCOSTO, " &
            "ROUND(IF(A.Exento = 0, A.precio1, A.precio1 * ((A.exento / 100) + 1)), 2) AS PVENTA FROM posarticulos A " &
            "INNER JOIN posarticuloslocal L ON L.carticulo = A.codigoart AND L.Clocal = @local " &
            "INNER JOIN posinventario I ON I.carticulo = A.codigoart AND I.Clocal = @local ORDER BY A.Descripcion"

        Public Async Function BuscarAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(SqlBuscar, New With {local}, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>El precio 1 con IVA, redondeado como en la caja.</summary>
        Public Shared Function PrecioVenta(a As PresentacionArticulo, factor As Double, direccion As DireccionRedondeo) As Double
            Dim precio = If(a.Exento = 0, a.Precio1, a.Precio1 * (a.Exento / 100 + 1))
            Return PreciosArticulo.Redondear(precio, factor, direccion)
        End Function

        ''' <summary>lblGramos: "PRECIO POR ML: 1.17"; vacío si el artículo no tiene presentación.</summary>
        Public Shared Function TextoPrecioPorUnidad(a As PresentacionArticulo, precio As Double, cultura As CultureInfo) As String
            If a.Grupo <= 0 Then Return ""
            Return $"PRECIO POR {a.Abrevia}: {(precio / a.Grupo).ToString("N2", cultura)}"
        End Function

        ''' <summary>El UPDATE de SD_GuardarArticulo; se replica como ARTICULOS.</summary>
        Friend Shared Function SentenciaGuardar(a As PresentacionArticulo) As String
            Return $"update posarticulos set DescCorta = {Q(a.DescCorta.Trim())},descripcion = {Q(a.Descripcion.Trim())},unidadmedida = {Q(a.UnidadMedida.Trim())}," &
                   $"grupo = '{a.Grupo.ToString("R", Invariante)}' where codigoart={Q(a.CodigoArt.Trim())};"
        End Function

        Public Async Function GuardarAsync(a As PresentacionArticulo, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim sql = SentenciaGuardar(a)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Await Replicador.ReplicarAsync(cn, ArticulosRepositorio.ReplicaArticulos, sql, local, ct).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
