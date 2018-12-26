# ludorill-server

## Mensajes procesados por el servidor
Los brackets (`{}`) alrededor de un nombre indican que es un valor que devuelve el servidor, sin embargo en el mensaje que envia solo va un valor sin brakcets (Por ejemplo al crear una partida el servidor envia: `S|MATCH|CREATED|1|0`).

Los nombres que comiencen por `:` representan parametros que debe enviar el cliente, sin incluir los dos puntos (Por ejemplo para crear una partida se enviaria `C|MATCH|CREATE|1`).

## Cuentas
Mensajes relacionados al manejo de cuentas de los usuarios.

### Registro
Para registrar a un usuario se debe enviar al servidor: `C|REGISTER|:username|:password`

Actualmente no se realizan validaciones a los parametros `username` y `password`.

**Respuestas del servidor:**

|Condición|Mensaje|Descripción|
| ------------- |:-----------------:|:-------------:|
|Success| `S\|REGISTER\|SUCCESS` | Registro completado exitosamente |
|Fail| `S\|REGISTER\|FAIL` | Registro fallido, actualmente solo se da si <br />el nombre de usuario ya esta en uso |

### Login
Para iniciar sesión en el servidor se debe enviar: `C|LOGIN|:username|:password`

**Respuestas del servidor:**

|Condición|Mensaje|Descripción|
| ------------- |:-----------------:|:-------------:|
|Success| `S\|LOGIN\|SUCCESS` | Sesión iniciada exitosamente |
|Fail| `S\|LOGIN\|FAIL` | Usuario o contraseña incorrecta |

## Partidas
Mensajes relacionados a la creación de partidas y el progreso de las partidas.

### Crear partida
Mensaje: `C|MATCH|CREATE|:animalSelection`

**Valores de parametro `animalSelection`:** 

|Animal| Valor |
|-------|:------|
| Elefantes |0|
| Jirafas |1|
| Vacas |2|
| Osos |3|

**Respuestas del servidor:**

|Condición|Mensaje|Descripción|
| ------------- |:-----------------:|:-------------:|
|Partida creada| `S\|MATCH\|CREATED\|{idPartida}\|{colorDelJugador}` | Se logro crear la partida,<br /> devuelve el id de la partida y el color que le corresponde al jugador que la creó (sin los brackets)|
|Usuario que intenta crear partida <br /> ya está en una en progreso| `S\|ERROR\|ALREADY_IN_MATCH` | No puede estar en varias partidas simultaneamente |
|Seleccion de animal invalida| `S\|ERROR\|INVALID_SELECTION` | El `:animalSelection` enviado no es un numero o se pasa del rango de opciones disponibles |
| Usuario sin sesion iniciada |`S\|ERROR\|NEEDS_LOGIN`| El cliente debe iniciar sesion para poder crear una partida |
| Error no manejado | `S\|ERROR\|UNKNOWN_ERROR` | Error no manejado directamente, pero se asume que no se logro crear la partida |
