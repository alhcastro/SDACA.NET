Imports System.IO
Imports CoopeNet.Core.Configuracion

Public Class EstadosVentanaTests

    <Fact>
    Public Sub Guarda_y_recupera_cada_pantalla()
        Dim ruta = Path.Combine(Path.GetTempPath(), $"ventanas-{Guid.NewGuid():N}.ini")
        Try
            Dim e = EstadosVentana.Cargar(ruta)
            Assert.False(e.Obtener("SD0502.FrmArticulos").HasValue)
            e.Recordar("SD0502.FrmArticulos", New EstadoVentana With {.Ancho = 1200, .Alto = 800, .Maximizada = True})
            e.Recordar("FrmBuscador", New EstadoVentana With {.Ancho = 700, .Alto = 500})
            e.Recordar("mala=clave", New EstadoVentana With {.Ancho = 1, .Alto = 1})
            e.Recordar("Vacia", New EstadoVentana With {.Ancho = 0, .Alto = 10})
            e.Guardar()
            Assert.Equal({"FrmBuscador=700,500,0", "SD0502.FrmArticulos=1200,800,1"}, File.ReadAllLines(ruta))
            Dim leido = EstadosVentana.Cargar(ruta).Obtener("sd0502.frmarticulos").Value
            Assert.Equal(1200, leido.Ancho)
            Assert.Equal(800, leido.Alto)
            Assert.True(leido.Maximizada)
        Finally
            File.Delete(ruta)
        End Try
    End Sub

    <Fact>
    Public Sub Ignora_lineas_que_no_entiende()
        Dim ruta = Path.Combine(Path.GetTempPath(), $"ventanas-{Guid.NewGuid():N}.ini")
        Try
            File.WriteAllLines(ruta, {"basura", "A=1,2", "B=x,2,0", "C=-5,2,0", "D=300,200,1"})
            Dim e = EstadosVentana.Cargar(ruta)
            Assert.Equal({"D=300,200,1"}, e.Lineas())
        Finally
            File.Delete(ruta)
        End Try
    End Sub

End Class
