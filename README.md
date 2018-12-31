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
| ------------- |:-----------------:|-------------|
|Success| `S\|REGISTER\|SUCCESS` | Registro completado exitosamente |
|Fail| `S\|REGISTER\|FAIL` | Registro fallido, actualmente solo se da si <br />el nombre de usuario ya esta en uso |

### Login
Para iniciar sesión en el servidor se debe enviar: `C|LOGIN|:username|:password`

**Respuestas del servidor:**

|Condición|Mensaje|Descripción|
| ------------- |:-----------------:|-------------|
|Success| `S\|LOGIN\|SUCCESS\|{listaDePartidas}` | Sesión iniciada exitosamente.<br/>Cada elemento de la lista de partidas tiene el siguiente formato: `matchId-numeroDeJugadores-nombreDePartida` y estan separados por comas (`,`)|
|Fail| `S\|LOGIN\|FAIL` | Usuario o contraseña incorrecta |

## Partidas
Mensajes relacionados a la creación de partidas y el progreso de las partidas.

**Errores manejados para todas las acciones de esta categoria:**

|Condición|Mensaje|Descripción|
| ------------- |:-----------------:|-------------|
| Usuario sin sesion iniciada |`S\|ERROR\|NEEDS_LOGIN`| El cliente debe iniciar sesion para poder ejecutar la accion |

### Crear partida
Mensaje: `C|MATCH|CREATE|:matchName`

**Valores validos `animalSelection`:** 

|Animal| Valor |
|:-------:|:-----:|
| Elefantes |0|
| Jirafas |1|
| Vacas |2|
| Osos |3|

**Respuestas del servidor:**

|Condición|Mensaje|Descripción|
| ------------- |:-----------------:|-------------|
|Partida creada| `S\|MATCH\|CREATED\|{idPartida}\|{usernameCreador}\|{colorDelJugador}\|{matchName}\|{animalCreador}` | Se logro crear la partida,<br /> devuelve el id de la partida, el username, color y animal del jugador que la creo, y el nombre de la partida. <br/>**Este mensaje se envia a todos los jugadores que hayan iniciado sesion**|
|Usuario que intenta crear partida <br /> ya está en una en progreso| `S\|ERROR\|ALREADY_IN_MATCH` | No puede estar en varias partidas simultaneamente |
| Error no manejado | `S\|ERROR\|UNKNOWN_ERROR` | Error no manejado directamente, pero se asume que no se logro crear la partida |

### Unirse a partida
Mensaje: `C|MATCH|JOIN|:matchId`

**Respuestas del servidor:**

|Condición|Mensaje|Descripción|
| ------------- |:-----------------:|-------------|
|Union a partida exitosa| `S\|MATCH\|JOINED\|{matchId}\|{playerUsername}\|{playerColor}\|{cantidadDeJugadores}\|{playerAnimal}`| **Esta respuesta se envia a todos los miembros de la partida**|
|ID de partida invalido|`S\|ERROR\|INVALID_MATCH_ID`| No existe una partida con el `matchId` recibido|
|Usuario ya esta en una partida| `S\|ERROR\|ALREADY_IN_MATCH` | No puede estar en varias partidas simultaneamente |

### Lanzar dado
Esta accion solo genera el numero al azar y lo guarda para ser ejecutado cuando se mande una accion `SELECT_PIECE` pues se debe seleccionar la pieza que sera movida.

Mensaje: `C|MATCH|PLAY|ROLL`

**Respuestas del servidor:**

|Condición|Mensaje|Descripción|
| ------------- |:-----------------:|-------------|
|Rolled successfully|`S\|MATCH\|PLAY\|ROLLED\|{matchId}\|{playerUsername}\|{playerColor}\|{diceRoll}\|{fichasMovibles}`|`playerUsername` es el nombre de usuario del jugador que lanzo el dado. <br />`diceRoll` es el numero que saco al lanzar el dado.<br /> `fichasMovibles` es una lista separada por comas de los indices de las piezas que puede mover el usuario (Cuales de las 4 piezas puede mover, por ejemplo: `0,1,3`).<br />**Este mensaje se manda a todos los jugadores de la partida**|
|Jugador no pertenece a partida|`S\|ERROR\|NOT_IN_MATCH`| El jugador no esta en ninguna partida |
|No es el turno del jugador| `S\|ERROR\|NOT_YOUR_TURN`||
|La partida no esta llena|`S\|ERROR\|MATCH_NOT_FULL`| Hecho para no permitir que los jugadores jueguen hasta que la partida se llene y comience|

### Seleccionar y mover pieza
Mensaje: `C|MATCH|PLAY|SELECT_PIECE|:indiceDePieza`

**Respuestas del servidor:**

|Condición|Mensaje|Descripción|
| ------------- |:-----------------:|-------------|
|Movimiento exitoso |`S\|MATCH\|PLAY\|MOVE\|{playerColor}\|{indiceDePieza}\|{numeroDeMovimientos}`| Indica cuantas casillas se debe mover una determinada pieza de un color a todos los miembros de la partida.<br/>**Este mensaje se manda a todos los jugadores de la partida**|
|Pieza seleccionada no se puede mover| `S\|ERROR\|UNMOVABLE_PIECE` | Las piezas que se pueden mover se reciben del servidor al lanzar el dado, donde se evalua cuales pueden ser movidas segun el numero que salio, por ejemplo si sale `6` puedes mover cualquier pieza|
|Jugador no pertenece a partida|`S\|ERROR\|NOT_IN_MATCH`| El jugador no esta en ninguna partida |
|No es el turno del jugador| `S\|ERROR\|NOT_YOUR_TURN`||
|La partida no esta llena|`S\|ERROR\|MATCH_NOT_FULL`| Hecho para no permitir que los jugadores jueguen hasta que la partida se llene y comience|
