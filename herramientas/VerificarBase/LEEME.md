# VerificarBase

Confirma contra el servidor de pruebas lo que Códigos, Cambio de Identificación y Fallecidos suponen del esquema. Solo ejecuta `SELECT` y `SHOW`. La contraseña se digita en la consola y no se guarda. El informe trae esquema, conteos y tipos de código, no datos de personas.

```bash
dotnet run --project herramientas/VerificarBase
```

Usa el perfil `WCENTRO_4` (puerto 3315). Para otro perfil, agrégalo al final: `dotnet run --project herramientas/VerificarBase -- NOMBRE`. El informe queda en `herramientas/VerificarBase/bin/Debug/net8.0/`, que git ignora.

## Prueba de las pantallas

Córrela antes y después: los conteos del informe deben quedar iguales, salvo `gatreplica`, que cambia solo. Las filas por tabla de la sección `gatcambioid` también pueden variar, porque el servidor es un espejo y sigue recibiendo datos. Entra en el **local 7**, que no tiene destinos en `gatreplicadef`, y usa identificaciones sintéticas. Crea primero dos personas en Datos Personales, A y B. Las tablas son MyISAM, así que nada se deshace solo: lo que crees lo borras al final.

**Administración de Códigos**

1. Buscar a A por identificación y por un código existente.
2. Agregarle un código y comprobar que aparece en la pestaña Códigos de Datos Personales.
3. Agregar el mismo código otra vez: debe rechazarlo.
4. Desde `FrmCodigoPersona`, abrir Tipos, crear un tipo, editarlo y borrarlo.
5. Intentar borrar un tipo en uso: no debe dejar.
6. Eliminar el código de A.

**Cambio de Identificación**

Usa identificaciones de 9 dígitos o más que no estén en uso. La única columna numérica de `gatcambioid` en el 3315 es `sgcempresas.UIDC`: MySQL compara ahí la identificación como número, y una corta podría coincidir con un usuario. En el paso 2, cancela en la confirmación: si se ejecuta, las tablas sin llave única pasan a B y `gacdp` no.

1. Cambiar A a una identificación sintética nueva y revisar el resumen del paso 4.
2. Intentar cambiar A a la identificación de B: debe avisar antes de confirmar.
3. Intentar usar la misma identificación original: debe rechazarla.
4. Devolver A a su identificación original.

**Fallecidos**

1. Registrar a B con una fecha: `fdef` queda con esa fecha y `activo` en 0.
2. Anular el registro: `fdef` vuelve a 1900-01-01 y `activo` a 1.
3. Buscar por código y por el buscador: las personas jurídicas no salen en el buscador.

**Al final:** borrar A y B, también sus filas en `gacdptel_bitac`, que llenan los triggers de `gacdptel`, y correr de nuevo el verificador.
