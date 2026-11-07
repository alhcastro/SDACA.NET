Namespace Pos

    ''' <summary>
    ''' Fila de posarticulos, con los nombres de la base para que Dapper la llene. Varias columnas se usan
    ''' para otra cosa que su nombre, igual que en VB6: CodAlterno es el código CABYS, Grupo la presentación,
    ''' SubGrupo la canasta básica, CodArtProv la marca, Recomendada el monto de otros impuestos, Exento el
    ''' porcentaje de IVA, Porcentaje el descuento máximo de venta, Gasto permite negativos y Consumo es No
    ''' facturar. Los precios se guardan sin IVA.
    ''' </summary>
    Public NotInheritable Class Articulo
        Public Property CodigoArt As String = ""
        Public Property CodAlterno As String = ""
        Public Property DescCorta As String = ""
        Public Property Descripcion As String = ""
        Public Property Ubicacion As String = ""
        Public Property Grupo As Double
        Public Property SubGrupo As Integer
        Public Property SinCantidad As String = "0"
        Public Property UnidadMedida As String = ""
        Public Property ValorEmpaque As String = ""
        Public Property Moneda As String = ""
        Public Property PrecioCosto As Double
        Public Property CostoPonderado As Double
        Public Property CostoPromedio As Double?
        Public Property Utilidad1 As Double
        Public Property Utilidad2 As Double
        Public Property Utilidad3 As Double
        Public Property Utilidad4 As Double
        Public Property Utilidad5 As Double
        Public Property Utilidad6 As Double
        Public Property Ex1 As Double
        Public Property Ex2 As Double
        Public Property Ex3 As Double
        Public Property Ex4 As Double
        Public Property Ex5 As Double
        Public Property Ex6 As Double
        Public Property Precio1 As Double
        Public Property Precio2 As Double
        Public Property Precio3 As Double
        Public Property Precio4 As Double
        Public Property Precio5 As Double
        Public Property Precio6 As Double
        Public Property Exento As Double
        Public Property Porcentaje As Double
        Public Property SinPeso As String = "0"
        Public Property PorcentajeOtros As Double
        Public Property Consignacion As String = "0"
        Public Property SinPrecio As String = "0"
        Public Property Gasto As String = "0"
        Public Property Servicio As String = "0"
        Public Property Descuento As Double
        Public Property Flete As Double
        Public Property Recomendada As Double
        Public Property DescEspecial As String = "0"
        Public Property NumDesc As Long
        Public Property Patrocinador As String = "0"
        Public Property NumPatroc As Long
        Public Property DescuentoA As Double
        Public Property CodArtProv As Integer
        Public Property Especificacion As String = ""
        Public Property Consumo As String = "0"
        Public Property FC As Date?
        Public Property FM As Date?
        Public Property Usuario As String = ""
        Public Property MXMayor As Long
        Public Property UsuarioCrea As String = ""

        ' ---- De otras tablas, solo para mostrar ----

        ''' <summary>El código corto (menos de 7 caracteres) de poscodart_codbarra en el local.</summary>
        Public Property CodigoCorto As String = ""
        ''' <summary>False si el artículo no está en posarticuloslocal para el local.</summary>
        Public Property Registrado As Boolean
        Public Property Stock As Double
        Public Property Categ As Integer
        Public Property SCateg As Integer
        Public Property SSCateg As Integer

        Public Function Utilidad(i As Integer) As Double
            Return {Utilidad1, Utilidad2, Utilidad3, Utilidad4, Utilidad5, Utilidad6}(i)
        End Function

        Public Function Extra(i As Integer) As Double
            Return {Ex1, Ex2, Ex3, Ex4, Ex5, Ex6}(i)
        End Function

        Public Function Precio(i As Integer) As Double
            Return {Precio1, Precio2, Precio3, Precio4, Precio5, Precio6}(i)
        End Function

        Public Sub FijarPrecios(utilidades As IReadOnlyList(Of Double), extras As IReadOnlyList(Of Double), precios As IReadOnlyList(Of Double))
            Utilidad1 = utilidades(0) : Utilidad2 = utilidades(1) : Utilidad3 = utilidades(2) : Utilidad4 = utilidades(3) : Utilidad5 = utilidades(4) : Utilidad6 = utilidades(5)
            Ex1 = extras(0) : Ex2 = extras(1) : Ex3 = extras(2) : Ex4 = extras(3) : Ex5 = extras(4) : Ex6 = extras(5)
            Precio1 = precios(0) : Precio2 = precios(1) : Precio3 = precios(2) : Precio4 = precios(3) : Precio5 = precios(4) : Precio6 = precios(5)
        End Sub

        Public Shared Function Marca(valor As Boolean) As String
            Return If(valor, "1", "0")
        End Function

        Public Shared Function EsMarcado(valor As String) As Boolean
            Return Vb6.Val(If(valor, "")) <> 0
        End Function
    End Class

    ''' <summary>Un código y su descripción para los combos del catálogo.</summary>
    Public NotInheritable Class OpcionArticulo
        Public Property Codigo As String = ""
        Public Property Descripcion As String = ""

        Public Overrides Function ToString() As String
            Return Descripcion
        End Function
    End Class

    ''' <summary>Lo que el catálogo lee una vez al abrir.</summary>
    Public NotInheritable Class CatalogosArticulo
        Public Property Unidades As New List(Of OpcionArticulo)
        Public Property Marcas As New List(Of OpcionArticulo)
        Public Property Monedas As New List(Of String)
        Public Property Ivas As New List(Of Double)
        ''' <summary>Las seis etiquetas de pos_etiquetaprecio0 a 5.</summary>
        Public Property EtiquetasPrecio As New List(Of String)
        ''' <summary>POSPrecioDia del día: el precio, de 1 a 6, que se destaca.</summary>
        Public Property PrecioDelDia As Integer = 1
        ''' <summary>POSFacRedon: el factor con que se redondea el precio del día que se muestra.</summary>
        Public Property FactorRedondeo As Double
        ''' <summary>PosUtilidadNeg: los usuarios que pueden guardar utilidades negativas.</summary>
        Public Property UsuariosUtilidadNegativa As String = ""
    End Class

End Namespace
