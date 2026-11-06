Imports System.Globalization
Imports Microsoft.VisualBasic

Namespace Bancos

    ''' <summary>Movimiento leído de un archivo del banco, listo para ccblibrobanco.</summary>
    Public NotInheritable Class MovimientoArchivo
        Public Property Fecha As Date
        Public Property Documento As String = ""
        Public Property Tipo As Integer
        Public Property TipoMov As Integer
        Public Property Monto As Double
        Public Property Detalle As String = ""
    End Class

    Public Enum EstadoArchivoTec
        NoValido
        SinCuenta
        Valido
    End Enum

    ''' <summary>
    ''' Archivo TEC del Banco Nacional. Migración de ReadTECFileBNCR y VerificarArchivoBNCR de
    ''' SD1401\FrmConciliacion2.frm: líneas de 120 caracteres con registros 0 (cuenta), 1 (movimiento) y 2 (fin).
    ''' </summary>
    Public NotInheritable Class ArchivoTec

        Private Const Largo As Integer = 120

        Private Sub New()
        End Sub

        Public Shared Function Verificar(lineas As IEnumerable(Of String), cuentaBanco As String) As EstadoArchivoTec
            Dim estado = EstadoArchivoTec.NoValido
            Dim tieneCuenta = False
            For Each linea In lineas.Where(Function(l) l.Length > 0)
                If "012".IndexOf(linea(0)) >= 0 Then
                    If linea.Length <> Largo Then Return EstadoArchivoTec.NoValido
                    estado = EstadoArchivoTec.Valido
                    If linea(0) = "0"c AndAlso Mid(linea, 2, 15) = cuentaBanco Then tieneCuenta = True
                ElseIf estado <> EstadoArchivoTec.Valido Then
                    Return EstadoArchivoTec.NoValido
                End If
            Next
            If estado = EstadoArchivoTec.Valido AndAlso Not tieneCuenta Then Return EstadoArchivoTec.SinCuenta
            Return estado
        End Function

        ''' <summary>Los movimientos de la cuenta. Un archivo trae varias: cada una termina con un registro 2.</summary>
        Public Shared Function Leer(lineas As IEnumerable(Of String), cuentaBanco As String) As List(Of MovimientoArchivo)
            Dim movimientos As New List(Of MovimientoArchivo)
            Dim enCuenta = False
            For Each linea In lineas.Where(Function(l) l.Length > 0)
                If Mid(linea, 2, 15) = cuentaBanco Then enCuenta = True
                If Not enCuenta Then Continue For
                If linea(0) = "2"c Then
                    enCuenta = False
                ElseIf linea(0) = "1"c Then
                    movimientos.Add(Movimiento(linea))
                End If
            Next
            Return movimientos
        End Function

        Private Shared Function Movimiento(linea As String) As MovimientoArchivo
            Dim tipoTec = CInt(Mid(linea, 2, 1))
            If tipoTec < 1 OrElse tipoTec > 4 Then Throw New FormatException($"Tipo de movimiento {tipoTec} desconocido.")
            Dim detalle = Mid(linea, 35, 30).Trim()
            Dim detalle2 = Mid(linea, 73, 48).Trim()
            Return New MovimientoArchivo With {
                .Fecha = New Date(CInt(Mid(linea, 15, 4)), CInt(Mid(linea, 13, 2)), CInt(Mid(linea, 11, 2))),
                .Documento = CLng(Conversion.Val(Mid(linea, 3, 8))).ToString(CultureInfo.InvariantCulture),
                .Tipo = If(tipoTec = 1 OrElse tipoTec = 3, 0, 1),
                .TipoMov = tipoTec - 1,
                .Monto = Double.Parse(Mid(linea, 19, 14) & "." & Mid(linea, 33, 2), CultureInfo.InvariantCulture),
                .Detalle = If(detalle2.Length > 0, detalle2, detalle)
            }
        End Function

    End Class

    ''' <summary>Columnas elegidas para un archivo CSV. Se guardan por banco en gatbancos.finport.</summary>
    Public NotInheritable Class ColumnasCsv
        Public Property Fecha As Integer
        Public Property Documento As Integer
        Public Property Detalle As Integer
        Public Property Debitos As Integer
        Public Property Creditos As Integer

        Public Shared Function DesdeFinport(texto As String) As ColumnasCsv
            Dim valor = If(texto, "").PadLeft(5, "0"c)
            Return New ColumnasCsv With {
                .Fecha = Digito(valor, 0), .Documento = Digito(valor, 1), .Detalle = Digito(valor, 2), .Debitos = Digito(valor, 3), .Creditos = Digito(valor, 4)
            }
        End Function

        Private Shared Function Digito(texto As String, posicion As Integer) As Integer
            Return If(Char.IsDigit(texto(posicion)), AscW(texto(posicion)) - AscW("0"c), 0)
        End Function

        ''' <summary>Como en VB6, solo caben las columnas A a J: finport guarda un dígito por columna.</summary>
        Public Function AFinport() As String
            Return String.Concat(Fecha, Documento, Detalle, Debitos, Creditos)
        End Function

        Public ReadOnly Property Repetidas As Boolean
            Get
                Return {Fecha, Documento, Detalle, Debitos, Creditos}.Distinct().Count() < 5
            End Get
        End Property
    End Class

    ''' <summary>Archivo CSV de los otros bancos. Migración de CCBCopyExcel de SD1401\FrmConciliacion.frm.</summary>
    Public NotInheritable Class ArchivoCsv

        Private Shared ReadOnly FormatosFecha As String() = {"d/M/yyyy", "d-M-yyyy", "d/M/yy", "d-M-yy", "yyyy-M-d", "yyyy/M/d", "d/MMM/yyyy", "d-MMM-yyyy"}
        Private Shared ReadOnly Cultura As New CultureInfo("es-CR")

        Private Sub New()
        End Sub

        Public Shared Function Separar(linea As String, separador As Char) As String()
            Dim campos As New List(Of String)
            Dim actual As New System.Text.StringBuilder()
            Dim comillas = False
            For Each c In linea
                If c = """"c Then
                    comillas = Not comillas
                ElseIf c = separador AndAlso Not comillas Then
                    campos.Add(actual.ToString())
                    actual.Clear()
                Else
                    actual.Append(c)
                End If
            Next
            campos.Add(actual.ToString())
            Return campos.ToArray()
        End Function

        ''' <summary>Una fila que no tenga fecha, documento numérico y monto se ignora, igual que en VB6.</summary>
        Public Shared Function Leer(filas As IEnumerable(Of String()), columnas As ColumnasCsv, inicio As Date, fin As Date) As List(Of MovimientoArchivo)
            Dim movimientos As New List(Of MovimientoArchivo)
            For Each fila In filas
                Dim fecha = LeerFecha(Celda(fila, columnas.Fecha), inicio, fin)
                Dim debito = Celda(fila, columnas.Debitos)
                Dim credito = Celda(fila, columnas.Creditos)
                Dim documento = Celda(fila, columnas.Documento)
                If Not fecha.HasValue OrElse Not EsNumero(documento) Then Continue For
                If Not EsNumero(debito) AndAlso Not EsNumero(credito) Then Continue For
                If Numero(debito) <= 0 AndAlso Numero(credito) <= 0 Then Continue For
                Dim tipo = If(EsNumero(debito) AndAlso Numero(debito) <> 0, 0, 1)
                movimientos.Add(New MovimientoArchivo With {
                    .Fecha = fecha.Value, .Documento = documento, .Tipo = tipo, .TipoMov = If(tipo = 1, 0, 1),
                    .Monto = Numero(If(tipo = 0, debito, credito)), .Detalle = Celda(fila, columnas.Detalle)
                })
            Next
            Return movimientos
        End Function

        Private Shared Function Celda(fila As String(), columna As Integer) As String
            Return If(columna < fila.Length, fila(columna).Trim(), "")
        End Function

        Private Shared Function EsNumero(texto As String) As Boolean
            Dim valor As Double
            Return Double.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, valor)
        End Function

        Private Shared Function Numero(texto As String) As Double
            Dim valor As Double
            Return If(Double.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, valor), valor, 0)
        End Function

        ''' <summary>Día/mes/año. Si cae fuera del rango se prueba con mes y día invertidos (VerificaFecha).</summary>
        Friend Shared Function LeerFecha(texto As String, inicio As Date, fin As Date) As Date?
            Dim fecha As Date
            If Not Date.TryParseExact(texto, FormatosFecha, Cultura, DateTimeStyles.None, fecha) Then Return Nothing
            If fecha >= inicio AndAlso fecha <= fin OrElse fecha.Day > 12 Then Return fecha
            Return New Date(fecha.Year, fecha.Day, fecha.Month)
        End Function

    End Class

End Namespace
