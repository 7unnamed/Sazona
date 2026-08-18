# Arquitectura del sistema

## 1. Vista general — cómo viaja una petición

Todo el tráfico entra por un único punto (el Gateway), que decide a qué servicio reenviar cada pedido según la ruta. Cada servicio tiene su propia base de datos — ninguno accede directamente a la base de otro.

```mermaid
flowchart LR
    Cliente(["Cliente<br/>(app / navegador / Postman)"])

    subgraph VPS["VPS (Docker)"]
        Gateway["API Gateway<br/>(YARP)"]

        Auth["Auth.Api<br/>login / registro"]
        Meals["Meals.Api<br/>platos e ingredientes"]
        Planner["Planner.Api<br/>historial de comidas"]

        subgraph PG["Postgres"]
            AuthDB[(auth_db)]
            MealsDB[(meals_db)]
            PlannerDB[(planner_db)]
        end
    end

    Cliente -- "/auth/*" --> Gateway
    Cliente -- "/meals/*" --> Gateway
    Cliente -- "/planner/*" --> Gateway

    Gateway -- "/auth/*" --> Auth
    Gateway -- "/meals/*" --> Meals
    Gateway -- "/planner/*" --> Planner

    Auth --- AuthDB
    Meals --- MealsDB
    Planner --- PlannerDB
```

**Cómo leerlo:** el cliente nunca le habla directo a `Meals.Api` o `Planner.Api` — todo pasa por el Gateway, que es el único servicio expuesto a internet. `Auth.Api` es quien entrega el token de sesión (JWT); `Meals.Api` y `Planner.Api` lo validan cada uno por su cuenta, sin volver a consultarle a `Auth.Api` en cada pedido.

## 2. Capas dentro de cada servicio

`Meals`, `Planner` y `Auth` están armados igual internamente — 4 capas, cada una en su propio proyecto, que solo pueden depender de la capa de abajo (nunca al revés):

```mermaid
flowchart TB
    Api["Api<br/><small>endpoints HTTP, arranque de la app</small>"]
    Application["Application<br/><small>reglas de negocio, qué datos entran/salen</small>"]
    Infrastructure["Infrastructure<br/><small>base de datos, migraciones</small>"]
    Domain["Domain<br/><small>las entidades del negocio (Plato, Usuario, ...)</small>"]

    Api --> Application
    Api --> Infrastructure
    Infrastructure --> Application
    Application --> Domain
```

**Por qué importa:** `Domain` no sabe que existe una base de datos ni HTTP — es el corazón del negocio, aislado. Si mañana se cambia de motor de base de datos, solo se toca `Infrastructure`; el resto ni se entera.

## 3. Lo que comparten los 3 servicios

Hay una pieza más, `BuildingBlocks`, que **no es un servicio** — es una caja de herramientas común para no repetir código de "plomería" (autenticación, auditoría, logs) en cada servicio:

```mermaid
flowchart LR
    subgraph BB["BuildingBlocks (compartido)"]
        direction TB
        BBAuth["Validación de JWT"]
        BBAudit["Auditoría automática<br/>(quién/cuándo creó, editó o borró algo)"]
        BBLog["Logging estructurado<br/>(Serilog)"]
    end

    Auth2["Auth.Api"] --> BB
    Meals2["Meals.Api"] --> BB
    Planner2["Planner.Api"] --> BB
```

Gracias a esto, agregar autenticación, auditoría o logs a un servicio **nuevo** el día de mañana es solo enchufarlo a `BuildingBlocks` — no hay que volver a programar nada de eso.

## 4. Qué pasa cuando se crea, edita o borra algo

Este es el mecanismo que registra automáticamente quién hizo qué y cuándo, sin que nadie tenga que escribirlo a mano en cada lugar:

```mermaid
sequenceDiagram
    participant U as Usuario (con token)
    participant E as Endpoint
    participant S as Servicio de negocio
    participant D as Base de datos

    U->>E: Pedido (crear / editar / borrar)
    E->>S: Ejecuta la acción
    S->>D: Guarda el cambio
    Note over D: Antes de guardar, se completa solo:<br/>quién lo hizo y en qué momento.<br/>Si es un borrado, no se elimina la fila:<br/>se marca como "borrada" y se oculta.
    D-->>U: Confirmación
```

## 5. Cómo se llega a producción

```mermaid
flowchart LR
    Dev["Cambios en el código"] --> Push["git push a main"]
    Push --> Tag["Se crea un tag<br/>(ej. v0.2.1)"]
    Tag --> CI["GitHub Actions<br/>se conecta al VPS por SSH"]
    CI --> Build["Reconstruye las imágenes<br/>Docker de los 3 servicios"]
    Build --> Up["Reinicia los contenedores<br/>con la nueva versión"]
```

Subir código a `main` **no despliega nada** — el despliegue solo ocurre cuando se crea un tag a propósito. Esto separa "guardar avances" de "publicar una versión nueva".
