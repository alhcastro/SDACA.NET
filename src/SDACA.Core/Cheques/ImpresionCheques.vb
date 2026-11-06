Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports SDACA.Core.Datos
Imports SDACA.Core.Impresion

Namespace Cheques

    Public NotInheritable Class ChequeParaImprimir
        Public Property Cheque As Long
        Public Property Fecha As Date
        Public Property Nombre As String = ""
        Public Property Monto As Double
        Public Property Observaciones As String = ""
        Public Property Id As String = ""
        Public Property Codigo As String = ""
        ''' <summary>Columna fm: la hora que se imprime en el cheque.</summary>
        Public Property Modificado As Date
        Public Property Usuario As String = ""
        Public Property Lineas As IReadOnlyList(Of LineaAsiento) = Array.Empty(Of LineaAsiento)()
    End Class

    Public NotInheritable Class TrabajoImpresion
        Public Property Texto As String = ""
        ''' <summary>Vacío: la impresora predeterminada de Windows.</summary>
        Public Property Impresora As String = ""
    End Class

    ''' <summary>Los datos que puede usar una plantilla de cheque. SDPrintChequei de SD1501\ModCheque.bas.</summary>
    Public NotInheritable Class DatosImpresionCheque

        Private Sub New()
        End Sub

        Public Shared Function Valores(c As ChequeParaImprimir, formato As FormatoCuenta) As Dictionary(Of String, String)
            Dim v As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Dim cultura = CultureInfo.CurrentCulture
            v("FechaLg") = c.Fecha.ToString("D", cultura)
            v("FechaSh") = c.Fecha.ToString("d", cultura)
            v("FechaF1") = c.Fecha.ToString("dd / MM / yyyy", cultura)
            v("FechaF2") = c.Fecha.ToString("dd / MMM / yyyy", cultura)
            v("FechaF3") = c.Fecha.ToString("dddd, dd / MMM / yyyy", cultura)
            v("FechaF4") = c.Fecha.ToString("MMMM dd,yyyy", cultura)
            v("Dia") = c.Fecha.ToString("dd", cultura)
            v("Mes") = c.Fecha.ToString("MMMM", cultura)
            v("Ano") = c.Fecha.ToString("yyyy", cultura)
            v("Hora") = c.Modificado.ToString("HH:mm:ss", cultura)
            v("Usuario") = c.Usuario
            v("MontoF1") = c.Monto.ToString("#,##0.00", cultura)
            v("MontoF2") = c.Monto.ToString("0.00", cultura)
            v("NombreF1") = FormatoCheque.Texto(c.Nombre, 0)
            v("NombreF2") = FormatoCheque.Texto(c.Nombre, 1)
            v("NombreF3") = FormatoCheque.Texto(c.Nombre, 2)
            v("ID") = c.Id
            v("Codigo") = c.Codigo
            Dim letras = MontoEnLetras.Convertir(c.Monto, True, True)
            v("MontoLF1") = FormatoCheque.Texto(letras, 0)
            v("MontoLF2") = FormatoCheque.Texto(letras, 1)
            v("MontoLF3") = FormatoCheque.Texto(letras, 2)
            v("Cheque") = c.Cheque.ToString(CultureInfo.InvariantCulture)

            Dim detalle = AcomodaDetalle(If(c.Observaciones, ""))
            Dim n = 0
            Dim corte = detalle.IndexOf(vbCrLf, StringComparison.Ordinal)
            While corte >= 0
                v("Detalle" & n) = detalle.Substring(0, corte)
                detalle = detalle.Substring(corte + 2)
                n += 1
                corte = detalle.IndexOf(vbCrLf, StringComparison.Ordinal)
            End While
            detalle = detalle.Replace(vbCr, "").Replace(vbLf, "")
            If detalle.Length > 0 Then v("Detalle" & n) = detalle

            For i = 0 To c.Lineas.Count - 1
                Dim l = c.Lineas(i)
                v("CC" & i) = formato.Mostrar(l.Cuenta)
                v("DCC" & i) = l.Detalle
                v("MDFA" & i) = If(l.Debe = 0, "", l.Debe.ToString("#,##0.00", cultura))
                v("MDFB" & i) = If(l.Debe = 0, "", l.Debe.ToString("0.00", cultura))
                v("MHFA" & i) = If(l.Haber = 0, "", l.Haber.ToString("#,##0.00", cultura))
                v("MHFB" & i) = If(l.Haber = 0, "", l.Haber.ToString("0.00", cultura))
            Next
            Return v
        End Function

        ''' <summary>
        ''' Parte el detalle en líneas de 110 caracteres, de preferencia en una coma. Migración literal:
        ''' cuando corta en una coma lejana conserva solo el final del texto acumulado, así que en ese
        ''' caso el original pierde el comienzo.
        ''' </summary>
        Public Shared Function AcomodaDetalle(detalle As String) As String
            Dim y = 0
            Dim texto = ""
            For Each c In detalle
                If c = vbCr OrElse c = vbLf Then y = -1
                If y = 110 Then
                    y = -1
                    If c = ","c Then
                        texto &= vbCrLf
                    Else
                        Dim coma = texto.LastIndexOf(","c) + 1
                        If coma > 95 Then
                            texto = texto.Substring(texto.Length - Math.Min(coma, texto.Length)) & vbCrLf
                        Else
                            texto &= vbCrLf
                        End If
                    End If
                End If
                texto &= c
                y += 1
            Next
            Return texto
        End Function

    End Class

    ''' <summary>El texto que muestra Impresión Masiva del cheque en curso. ECCLoadCheque de FrmPrintChequeM.frm.</summary>
    Public NotInheritable Class ResumenCheque

        Private Sub New()
        End Sub

        Public Shared Function Armar(c As ChequeParaImprimir, formato As FormatoCuenta) As String
            Dim t As New Text.StringBuilder()
            t.Append("IMPRIMIENDO CHEQUE # ").Append(c.Cheque).Append(vbCrLf)
            t.Append("A Nombre de:").Append(c.Nombre).Append(vbCrLf)
            t.Append("Con Fecha:").Append(c.Fecha.ToString("d", CultureInfo.CurrentCulture)).Append(vbCrLf)
            t.Append("Por el Monto de:").Append(c.Monto.ToString("N2", CultureInfo.CurrentCulture))
            t.Append("(").Append(MontoEnLetras.Convertir(c.Monto, True, True)).Append(")").Append(vbCrLf).Append(vbCrLf)
            t.Append("DETALLE:").Append(vbCrLf).Append(c.Observaciones).Append(vbCrLf).Append(vbCrLf)
            If c.Lineas.Count = 0 Then Return t.ToString()

            t.Append("DETALLE CONTABLE").Append(vbCrLf)
            t.Append(Columnas("Cuenta No.", "Detalle", "Debe", "Haber"))
            t.Append(New String("="c, 83)).Append(vbCrLf)
            For Each l In c.Lineas
                t.Append(Columnas(formato.Mostrar(l.Cuenta.Trim()), l.Detalle.Trim(), Importe(l.Debe), Importe(l.Haber)))
            Next
            Return t.ToString()
        End Function

        Private Shared Function Columnas(cuenta As String, detalle As String, debe As String, haber As String) As String
            Return PlantillaImpresion.Ajusta(cuenta, 17, "I"c) & PlantillaImpresion.Ajusta(detalle, 35, "I"c) &
                   PlantillaImpresion.Ajusta(debe, 15, "D"c) & PlantillaImpresion.Ajusta(haber, 16, "D"c) & vbCrLf
        End Function

        Private Shared Function Importe(valor As Double) As String
            Return If(valor = 0, "", valor.ToString("N2", CultureInfo.CurrentCulture))
        End Function

    End Class

    ''' <summary>
    ''' Arma el cheque para la impresora. Solo el formato de texto de gatprintapi: el parámetro CHKPAPI
    ''' está en todos los locales, así que el reporte gráfico con eccchequesconfig no se usa y no se migró.
    ''' </summary>
    Public NotInheritable Class ImpresionCheques

        Private ReadOnly _fabrica As IFabricaConexiones
        Private ReadOnly _parametros As Parametros

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
            _parametros = New Parametros(fabrica)
        End Sub

        ''' <summary>ECCPrintCheque con un valor distinto de 0 apaga la impresión.</summary>
        Public Async Function SeImprimeAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Return Await _parametros.ObtenerNumeroAsync(local, "ECCPrintCheque", ct).ConfigureAwait(False) = 0
        End Function

        Public Async Function UsaPlantillaAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Return (Await _parametros.ObtenerAsync(local, "CHKPAPI", ct).ConfigureAwait(False)).Length > 0
        End Function

        Public Async Function ArmarAsync(banco As Integer, cuenta As String, numero As Long, local As Long, estacion As String, formato As FormatoCuenta, Optional ct As CancellationToken = Nothing) As Task(Of TrabajoImpresion)
            Const sqlCheque As String =
                "SELECT a.cheque AS Cheque, a.fecha AS Fecha, a.nombre AS Nombre, a.monto AS Monto, a.observaciones AS Observaciones, " &
                "a.id AS Id, a.codigo AS Codigo, a.fm AS Modificado, c.usuario AS Usuario FROM ecccheques a " &
                "LEFT JOIN gatusuarios c ON c.uid = a.uidc WHERE a.banco = @banco AND a.cuenta = @cuenta AND a.cheque = @numero"
            Const sqlAsiento As String =
                "SELECT ccontable AS Cuenta, detalle AS Detalle, md AS Debe, mh AS Haber FROM eccchequesconta " &
                "WHERE banco = @banco AND cuenta = @cuenta AND cheque = @numero ORDER BY pos"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim cheque = Await cn.QueryFirstOrDefaultAsync(Of ChequeParaImprimir)(New CommandDefinition(sqlCheque, New With {banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)
                If cheque Is Nothing Then Return Nothing
                cheque.Lineas = (Await cn.QueryAsync(Of LineaAsiento)(New CommandDefinition(sqlAsiento, New With {banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                Dim plantilla = Await CargarPlantillaAsync(cn, banco, cuenta, local, ct).ConfigureAwait(False)
                Return New TrabajoImpresion With {
                    .Texto = PlantillaImpresion.Armar(plantilla, DatosImpresionCheque.Valores(cheque, formato)),
                    .Impresora = Await BuscarImpresoraAsync(cn, local, estacion, cuenta, ct).ConfigureAwait(False)
                }
            End Using
        End Function

        ''' <summary>El cheque si existe y no está anulado. Sus líneas van por System y posición, como en pantalla.</summary>
        Public Async Function ObtenerVigenteAsync(banco As Integer, cuenta As String, numero As Long, Optional ct As CancellationToken = Nothing) As Task(Of ChequeParaImprimir)
            Const sqlCheque As String = "SELECT cheque AS Cheque, fecha AS Fecha, nombre AS Nombre, monto AS Monto, CAST(observaciones AS CHAR) AS Observaciones " &
                                        "FROM ecccheques WHERE cuenta = @cuenta AND banco = @banco AND cheque = @numero AND anulado = '0'"
            Const sqlAsiento As String = "SELECT ccontable AS Cuenta, detalle AS Detalle, md AS Debe, mh AS Haber FROM eccchequesconta " &
                                         "WHERE cuenta = @cuenta AND banco = @banco AND cheque = @numero ORDER BY System, pos"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim cheque = Await cn.QueryFirstOrDefaultAsync(Of ChequeParaImprimir)(New CommandDefinition(sqlCheque, New With {banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)
                If cheque Is Nothing Then Return Nothing
                cheque.Lineas = (Await cn.QueryAsync(Of LineaAsiento)(New CommandDefinition(sqlAsiento, New With {banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                Return cheque
            End Using
        End Function

        ''' <summary>El formato de la cuenta, o si no tiene, el del banco. En cada caso, el del local o el general.</summary>
        Private Shared Async Function CargarPlantillaAsync(cn As MySqlConnection, banco As Integer, cuenta As String, local As Long, ct As CancellationToken) As Task(Of List(Of LineaPlantilla))
            Const existe As String = "SELECT informe FROM gatprintapi WHERE informe = @informe LIMIT 1"
            Const lineas As String = "SELECT Linea, Multiple FROM gatprintapi WHERE local = @origen AND informe = @informe ORDER BY pos"
            Dim informe = "CHK" & cuenta
            Dim hallado = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(existe, New With {informe}, cancellationToken:=ct)).ConfigureAwait(False)
            If String.IsNullOrWhiteSpace(hallado) Then informe = "CHK" & banco.ToString(CultureInfo.InvariantCulture)
            For Each origen In {local, -1L}
                Dim filas = (Await cn.QueryAsync(Of LineaPlantilla)(New CommandDefinition(lineas, New With {origen, informe}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                If filas.Count > 0 Then Return filas
            Next
            ' El original se queda en un ciclo infinito si el formato no existe.
            Throw New InvalidOperationException($"No existe el formato de impresión {informe}.")
        End Function

        ''' <summary>CHK_GetPrinter: la impresora de la cuenta o, si no tiene, la de CHEQUES.</summary>
        Private Shared Async Function BuscarImpresoraAsync(cn As MySqlConnection, local As Long, estacion As String, cuenta As String, ct As CancellationToken) As Task(Of String)
            Const sql As String = "SELECT impresora FROM gatimpresion WHERE local = @l AND estacion = @e AND documento = @documento LIMIT 1"
            For Each documento In {"CHK" & cuenta.Trim(), "CHEQUES"}
                For Each origen In {(l:=local, e:=estacion), (l:=-1L, e:=estacion), (l:=local, e:="-1"), (l:=-1L, e:="-1")}
                    Dim impresora = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sql, New With {origen.l, origen.e, documento}, cancellationToken:=ct)).ConfigureAwait(False)
                    If Not String.IsNullOrEmpty(impresora) Then Return ImpresoraLocal(impresora, Environment.MachineName)
                Next
            Next
            Return ""
        End Function

        ''' <summary>Una impresora compartida desde esta misma PC se usa por su nombre local.</summary>
        Public Shared Function ImpresoraLocal(impresora As String, equipo As String) As String
            If impresora.IndexOf("\\" & equipo, StringComparison.OrdinalIgnoreCase) < 0 Then Return impresora
            Return impresora.Substring(impresora.LastIndexOf("\"c) + 1)
        End Function

    End Class

End Namespace
