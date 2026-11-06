Imports System.ComponentModel

''' <summary>Botones de la barra. Mismos valores que Enum_SDBoton de SDCMD.ocx.</summary>
Public Enum BotonBarra
    Nuevo = 0
    Guardar = 1
    Borrar = 2
    Buscar = 3
    Imprimir = 4
    Editar = 5
    Cancelar = 6
    Salir = 7
End Enum

''' <summary>Mismos valores que Enum_SDModo de SDCMD.ocx.</summary>
Public Enum ModoBarra
    Normal = 0
    Buscar = 2
    Imprimir = 4
    Todos = 8
End Enum

''' <summary>
''' Reemplaza a SDCMD.ocx (ActiveX\Cmdbar\SDCMD.ctl). Conserva su máquina de estados: Nuevo y
''' Editar convierten los botones Nuevo y Salir en Guardar y Cancelar, y apagan los demás; Guardar
''' y Cancelar devuelven la barra a su estado normal.
''' </summary>
Public Class BarraComandos
    Inherits UserControl

    Private ReadOnly _botones As New List(Of Button)
    Private ReadOnly _ayuda As New ToolTip()
    Private ReadOnly _permitido As New Dictionary(Of BotonBarra, Boolean)
    Private _modo As ModoBarra = ModoBarra.Buscar

    ''' <summary>
    ''' Equivale al evento Click del OCX. Devuelve True para cancelar, igual que Cancel = True:
    ''' la barra no cambia de estado. Es una función asíncrona porque casi todas las acciones
    ''' van a la base, y la barra no debe cambiar de estado hasta que terminen.
    ''' </summary>
    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property AlHacerClic As Func(Of BotonBarra, Task(Of Boolean))

    Public Sub New()
        CargarBotones()
    End Sub

    <DefaultValue(ModoBarra.Buscar)>
    Public Property Modo As ModoBarra
        Get
            Return _modo
        End Get
        Set(value As ModoBarra)
            _modo = value
            CargarBotones()
        End Set
    End Property

    ''' <summary>Igual que <c>Estado</c> del OCX: permite o impide un botón, sin importar el estado de la barra.</summary>
    Public Sub Estado(boton As BotonBarra, habilitado As Boolean)
        _permitido(boton) = habilitado
        Refrescar(EnEdicion)
    End Sub

    ''' <summary>Hace clic en un botón por código, igual que <c>Accion</c> del OCX.</summary>
    Public Sub Accion(boton As BotonBarra)
        Dim b = _botones.FirstOrDefault(Function(x) CType(x.Tag, BotonBarra) = boton AndAlso x.Enabled)
        b?.PerformClick()
    End Sub

    ''' <summary>True mientras los botones muestran Guardar y Cancelar.</summary>
    <Browsable(False)>
    Public ReadOnly Property EnEdicion As Boolean
        Get
            Return _botones.Any(Function(b) CType(b.Tag, BotonBarra) = BotonBarra.Guardar)
        End Get
    End Property

    Private Sub CargarBotones()
        For Each b In _botones
            Controls.Remove(b)
            b.Dispose()
        Next
        _botones.Clear()
        Dim tipos As New List(Of BotonBarra) From {BotonBarra.Nuevo, BotonBarra.Borrar}
        If _modo = ModoBarra.Buscar OrElse _modo = ModoBarra.Todos Then tipos.Add(BotonBarra.Buscar)
        If _modo = ModoBarra.Imprimir OrElse _modo = ModoBarra.Todos Then tipos.Add(BotonBarra.Imprimir)
        tipos.Add(BotonBarra.Editar)
        tipos.Add(BotonBarra.Salir)
        For Each tipo In tipos
            Dim b As New Button With {.Tag = tipo, .UseVisualStyleBackColor = True}
            AddHandler b.Click, AddressOf Boton_Click
            _botones.Add(b)
            Controls.Add(b)
        Next
        Acomodar()
        Refrescar(False)
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        Acomodar()
    End Sub

    Private Sub Acomodar()
        If _botones.Count = 0 Then Return
        Dim ancho = ClientSize.Width \ _botones.Count
        For i = 0 To _botones.Count - 1
            Dim ultimo = i = _botones.Count - 1
            _botones(i).SetBounds(i * ancho, 0, If(ultimo, ClientSize.Width - i * ancho, ancho), ClientSize.Height)
        Next
    End Sub

    Private Async Sub Boton_Click(sender As Object, e As EventArgs)
        Dim tipo = CType(CType(sender, Button).Tag, BotonBarra)
        Dim cancelado = False
        Enabled = False
        Try
            If AlHacerClic IsNot Nothing Then cancelado = Await AlHacerClic(tipo)
        Finally
            If Not IsDisposed Then Enabled = True
        End Try
        If cancelado OrElse IsDisposed Then Return
        Select Case tipo
            Case BotonBarra.Nuevo, BotonBarra.Editar
                Cambiar(BotonBarra.Nuevo, BotonBarra.Guardar)
                Cambiar(BotonBarra.Salir, BotonBarra.Cancelar)
                Refrescar(True)
            Case BotonBarra.Guardar, BotonBarra.Cancelar
                Cambiar(BotonBarra.Guardar, BotonBarra.Nuevo)
                Cambiar(BotonBarra.Cancelar, BotonBarra.Salir)
                Refrescar(False)
        End Select
    End Sub

    Private Sub Cambiar(origen As BotonBarra, nuevo As BotonBarra)
        For Each b In _botones.Where(Function(x) CType(x.Tag, BotonBarra) = origen)
            b.Tag = nuevo
        Next
    End Sub

    Private Sub Refrescar(editando As Boolean)
        For Each b In _botones
            Dim tipo = CType(b.Tag, BotonBarra)
            b.Text = Descripcion(tipo)
            _ayuda.SetToolTip(b, Ayuda(tipo))
            Dim activo = Not editando OrElse tipo = BotonBarra.Guardar OrElse tipo = BotonBarra.Cancelar
            Dim permitido As Boolean = True
            If _permitido.TryGetValue(tipo, permitido) AndAlso Not permitido Then activo = False
            b.Enabled = activo
        Next
    End Sub

    Private Shared Function Descripcion(boton As BotonBarra) As String
        Select Case boton
            Case BotonBarra.Nuevo : Return "&Nuevo"
            Case BotonBarra.Guardar : Return "&Guardar"
            Case BotonBarra.Borrar : Return "Borrar"
            Case BotonBarra.Buscar : Return "&Buscar"
            Case BotonBarra.Imprimir : Return "&Imprimir"
            Case BotonBarra.Editar : Return "&Editar"
            Case BotonBarra.Cancelar : Return "&Cancelar"
            Case Else : Return "&Salir"
        End Select
    End Function

    Private Shared Function Ayuda(boton As BotonBarra) As String
        Select Case boton
            Case BotonBarra.Nuevo : Return "Agrega un nuevo registro a la base de datos"
            Case BotonBarra.Guardar : Return "Graba la información nueva o modificada en la base de datos"
            Case BotonBarra.Borrar : Return "Borra la información seleccionada"
            Case BotonBarra.Buscar : Return "Buscar un registro en la base de datos"
            Case BotonBarra.Imprimir : Return "Imprime la información seleccionada"
            Case BotonBarra.Editar : Return "Modifica la información seleccionada"
            Case BotonBarra.Cancelar : Return "Cancela la acción actual"
            Case Else : Return "Sale del proceso en curso"
        End Select
    End Function

End Class
