# Guía del proyecto ComidaDiaria — cómo se armó, desde cero

Esta guía explica, como si fuera una clase, todo lo que se hizo para levantar este backend: por qué cada pieza está donde está y qué problema resuelve. Sirve como referencia para reconstruir el proyecto o para entender las decisiones si vuelves dentro de unos meses.

## 1. Arquitectura general

La idea del proyecto es un **monolito modular pensado para evolucionar a microservicios**. Eso significa: hoy son servicios separados que corren de forma independiente (cada uno con su propia base de datos), pero conviven en una sola solución para simplificar el desarrollo mientras el proyecto es pequeño.

```
ComidaDiaria.slnx
├── src/Services/Meals.Api      → gestiona Platos e Ingredientes
├── src/Services/Planner.Api    → gestiona el historial de comidas planificadas
└── src/Gateway/ApiGateway      → puerta de entrada única (YARP reverse proxy)
```

Reglas de diseño que se siguieron:

- **Cada servicio tiene su propia base de datos** (`meals_db`, `planner_db`). Esto es clave en microservicios: nunca comparten tablas directamente. Si `Planner.Api` necesita saber de un `Plato`, no hace un JOIN a la tabla de `Meals.Api` — guarda solo el `IdPlato` como referencia (ver sección 3).
- **EF Core + LINQ**, sin Dapper. Se prioriza productividad y migraciones automáticas sobre control fino de SQL.
- **Capas ligeras** dentro de cada servicio: por ahora `Domain/` (entidades) y `Data/` (DbContext). Se irán agregando `Application/` e `Infrastructure/` cuando aparezca lógica de negocio real.
- **Nombres descriptivos**: en vez de `Id` o `Nombre` a secas, se usa `IdPlato`, `NombrePlato`, etc. Esto evita ambigüedad cuando una clase tiene varias relaciones (por ejemplo, `Ingrediente` tiene `IdIngrediente` propio e `IdPlato` de la relación — sin este criterio sería fácil confundirlos).

## 2. La herramienta `dotnet-ef` y el problema inicial

**Qué es:** `dotnet-ef` es la CLI que genera y aplica migraciones de Entity Framework Core (`dotnet ef migrations add`, `dotnet ef database update`). No viene con el SDK de .NET, hay que instalarla como *global tool*.

**El error que apareció:**
```
dotnet tool install --global dotnet-ef
Unhandled exception: El archivo de configuración DotnetToolSettings.xml no se encontró en el paquete.
```

**Diagnóstico:** al inspeccionar `C:\Users\<usuario>\.dotnet\tools\.store\dotnet-ef`, se encontró que la herramienta **ya estaba instalada** (versión 10.0.0) de un intento anterior, con el `DotnetToolSettings.xml` presente y todo el contenido extraído correctamente. El error no era de fuentes NuGet ni de conectividad — era que `install` fallaba al intentar reinstalar sobre algo que ya existía. Se confirmó que funcionaba con:
```
dotnet-ef --version
```
que respondió `10.0.0` sin problema. Moraleja: **antes de reinstalar algo que da error, revisa si ya está instalado.**

Intentar `dotnet tool update --global dotnet-ef` para alinear la versión con el runtime (10.0.11) dio el mismo error — quedó pendiente, pero no bloquea nada porque 10.0.0 funciona bien contra SDK 10.0.400.

## 3. Entidades y DbContext de `Meals.Api`

**Carpeta `Domain/Enums/TipoComida.cs`** — un enum separado (no vive dentro de la entidad) para poder reutilizarlo y mantener el archivo de la entidad enfocado solo en sus propiedades:
```csharp
public enum TipoComida { Desayuno, Almuerzo, Cena, Snack }
```

**Carpeta `Domain/Plato.cs`:**
```csharp
public class Plato
{
    public int IdPlato { get; set; }
    public string NombrePlato { get; set; } = string.Empty;
    public TipoComida TipoComida { get; set; }
    public int PorcionesBase { get; set; }
    public ICollection<Ingrediente> Ingredientes { get; set; } = new List<Ingrediente>();
}
```

**Carpeta `Domain/Ingrediente.cs`:**
```csharp
public class Ingrediente
{
    public int IdIngrediente { get; set; }
    public string NombreIngrediente { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public int IdPlato { get; set; }
    public Plato Plato { get; set; } = null!;
}
```
`Cantidad` es `decimal` (no `int`) porque una receta puede pedir `0.5` kg o `1.5` tazas — con `int` se perdería precisión.

**Relación:** un `Plato` tiene muchos `Ingredientes` (1:N), configurada en `Data/MealsDbContext.cs` con Fluent API en `OnModelCreating`:
```csharp
modelBuilder.Entity<Ingrediente>(entity =>
{
    entity.HasOne(i => i.Plato)
        .WithMany(p => p.Ingredientes)
        .HasForeignKey(i => i.IdPlato)
        .OnDelete(DeleteBehavior.Cascade); // si se borra el Plato, se borran sus Ingredientes
});
```

## 4. Entidad y DbContext de `Planner.Api`

**`Domain/HistorialEntry.cs`:**
```csharp
public class HistorialEntry
{
    public int IdHistorialEntry { get; set; }
    public int IdPlato { get; set; }       // referencia "suelta" al Plato de Meals.Api
    public DateOnly Fecha { get; set; }
    public TipoComida TipoComida { get; set; }
    public bool Confirmado { get; set; }
}
```

Nota clave: `IdPlato` **no tiene navegación** (`Plato Plato { get; set; }`) como sí la tenía `Ingrediente`. Esto es intencional: `Plato` vive en la base de datos de `Meals.Api`, un servicio distinto. En una arquitectura de microservicios de verdad, un `DbContext` nunca debe intentar hacer un JOIN a través de una base de datos ajena — si `Planner.Api` necesita los datos del plato, los pediría vía HTTP al propio `Meals.Api` (eso queda pendiente para cuando haya lógica de negocio real).

`Confirmado` tiene un valor por defecto `false` en la base de datos vía `HasDefaultValue(false)`, para que insertar una entrada de historial sin especificar el campo no falle.

## 5. Conexión a Postgres (Npgsql)

En cada servicio, dentro de `Program.cs`:
```csharp
builder.Services.AddDbContext<MealsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MealsDb")));
```

La connection string se lee de la sección `ConnectionStrings` de `appsettings.json`. .NET tiene un sistema de configuración en capas: `appsettings.json` es la base, y `appsettings.Development.json` **sobreescribe** esos valores solo cuando `ASPNETCORE_ENVIRONMENT=Development` (que es el valor por defecto al correr con `dotnet run` en local). Por eso hay dos connection strings distintas para cada servicio:

- **`appsettings.json`** (base / producción): `Host=postgres;...` — porque cuando el backend se despliegue como contenedor Docker en el mismo VPS, "postgres" será resoluble por nombre dentro de la red Docker `app-network` (Docker tiene su propio DNS interno entre contenedores de una misma red).
- **`appsettings.Development.json`** (tu PC): `Host=localhost;...` — porque desde tu máquina te conectas a Postgres a través de un túnel SSH que expone el puerto localmente (ver sección 7).

## 6. Migraciones

Con `dotnet-ef` funcionando, se generó la migración inicial en cada servicio (esto **no requiere conexión activa a la base de datos** — solo lee el modelo del `DbContext` y genera el código C# + SQL de la migración):

```bash
cd src/Services/Meals.Api
dotnet ef migrations add InitialCreate

cd src/Services/Planner.Api
dotnet ef migrations add InitialCreate
```

Esto crea una carpeta `Migrations/` en cada proyecto con el snapshot del modelo y el script de creación de tablas. **Aplicar** la migración (`dotnet ef database update`) sí requiere una base de datos real y accesible — por eso se pospuso hasta tener el VPS listo (sección 7).

## 7. El VPS: Postgres en Docker

### 7.1 Qué hay en el VPS

Una VM Debian 12 en Azure (North Central US, IP `20.88.21.217`) con Docker y Docker Compose instalados. Ahí se creó:

```
~/comidiaria/postgres/
├── docker-compose.yml
└── init-databases.sh
```

**`docker-compose.yml`** (resumen):
```yaml
services:
  postgres:
    image: postgres:16
    container_name: comidiaria-postgres
    environment:
      POSTGRES_USER: comidiaria_admin
      POSTGRES_PASSWORD: <password generada>
      POSTGRES_DB: postgres
    ports:
      - "127.0.0.1:5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./init-databases.sh:/docker-entrypoint-initdb.d/init-databases.sh
    networks:
      - app-network
```

Puntos importantes de este archivo:

- **`ports: "127.0.0.1:5432:5432"`** — el puerto solo se publica en la interfaz *loopback* del VPS (`127.0.0.1`), NO en `0.0.0.0`. Esto significa que **nadie desde internet puede conectarse directo** al puerto 5432 del VPS, ni siquiera si el firewall lo dejara pasar — el propio Postgres solo escucha localmente. La única forma de llegar es estando físicamente "dentro" del VPS o mediante un túnel SSH (sección 7.3).
- **`volumes: pgdata`** — un volumen Docker con nombre, para que los datos de la base sobrevivan si el contenedor se borra o se actualiza la imagen.
- **`networks: app-network`** — una red Docker dedicada. Cuando más adelante despleguemos `Meals.Api` y `Planner.Api` como contenedores en el mismo VPS, se unirán a esta misma red y podrán resolver el contenedor de Postgres por su nombre de servicio (`postgres`), sin exponer nada a internet.

**`init-databases.sh`** — un script que Postgres ejecuta automáticamente la primera vez que arranca (por convención, todo lo que está en `/docker-entrypoint-initdb.d/` se corre al iniciar con un volumen vacío). Crea las dos bases de datos separadas:
```bash
CREATE DATABASE meals_db;
CREATE DATABASE planner_db;
```

### 7.2 Cómo se levantó

Desde la máquina local, por SSH con la llave `.pem`:
```bash
ssh -i "ruta\a\PrimeraPruebaAzure_key.pem" azureuser@20.88.21.217 "cd ~/comidiaria/postgres && docker compose up -d"
```
Se verificó con `docker compose ps` que el contenedor quedó `Up`, y con `psql -U comidiaria_admin -d postgres -c '\l'` que ambas bases existían.

### 7.3 El túnel SSH para desarrollo local

Como Postgres solo escucha en `127.0.0.1` **dentro** del VPS, para conectarnos desde la PC local se usa un **túnel SSH**: SSH crea una conexión cifrada hacia el VPS y "reenvía" un puerto local tuyo hacia el puerto del VPS, como si estuvieran en la misma red.

```bash
ssh -i "ruta\a\PrimeraPruebaAzure_key.pem" -f -N -L 5432:localhost:5432 azureuser@20.88.21.217
```

Desglose de las banderas:
- `-L 5432:localhost:5432` — "todo lo que llegue a mi puerto local 5432, reenvíalo al `localhost:5432` **visto desde el VPS**" (que es justo donde Postgres escucha).
- `-f -N` — corre el túnel en segundo plano (`-f`) sin abrir una sesión de shell interactiva (`-N`), porque solo lo necesitamos como "cañería", no para ejecutar comandos.

Mientras este túnel esté activo, cualquier app en tu PC que se conecte a `localhost:5432` en realidad está hablando con el Postgres del VPS. Por eso `appsettings.Development.json` usa `Host=localhost`. **Si reinicias tu PC, el túnel se cae y hay que volver a correr ese comando** antes de trabajar con la base de datos.

### 7.4 Aplicar las migraciones al VPS

Con el túnel activo, se corrió lo mismo que en la sección 6 pero con `database update`, que sí ejecuta el SQL contra la base real:
```bash
cd src/Services/Meals.Api
dotnet ef database update

cd src/Services/Planner.Api
dotnet ef database update
```
Se verificaron las tablas resultantes con `\dt` desde `psql` dentro del contenedor: `Platos`, `Ingredientes`, `HistorialEntries`, y la tabla interna `__EFMigrationsHistory` que EF usa para saber qué migraciones ya se aplicaron.

## 8. El Gateway (YARP)

`ApiGateway` es el único punto de entrada público pensado para el futuro (hoy en día `Meals.Api` y `Planner.Api` también son accesibles directo en sus puertos, pero la idea es que en producción solo el Gateway esté expuesto).

**YARP** (Yet Another Reverse Proxy) es la librería de Microsoft para armar un reverse proxy en código .NET. Se configura declarativamente en `appsettings.json`, con dos conceptos:

- **Route** — qué patrón de URL entra y a qué *cluster* se manda.
- **Cluster** — el conjunto de destinos (servidores reales) a los que se reenvía la petición.

```json
"ReverseProxy": {
  "Routes": {
    "meals-route": {
      "ClusterId": "meals-cluster",
      "Match": { "Path": "/meals/{**catch-all}" },
      "Transforms": [ { "PathRemovePrefix": "/meals" } ]
    },
    "planner-route": {
      "ClusterId": "planner-cluster",
      "Match": { "Path": "/planner/{**catch-all}" },
      "Transforms": [ { "PathRemovePrefix": "/planner" } ]
    }
  },
  "Clusters": {
    "meals-cluster":   { "Destinations": { "meals-api":   { "Address": "http://localhost:5094" } } },
    "planner-cluster":  { "Destinations": { "planner-api": { "Address": "http://localhost:5062" } } }
  }
}
```

**Cómo leerlo:**
- `"Match": { "Path": "/meals/{**catch-all}" }` — cualquier URL que empiece con `/meals/` (el `{**catch-all}` captura el resto del path, sin importar cuántos segmentos tenga).
- `"Transforms": [ { "PathRemovePrefix": "/meals" } ]` — **esta es la línea que tenías seleccionada**. Antes de reenviar la petición, le quita el prefijo `/meals` a la URL. Por ejemplo, una petición a `GATEWAY/meals/platos/3` llega a `Meals.Api` como `/platos/3` — el servicio interno no necesita saber que existe un prefijo `/meals`, eso es un detalle de cómo lo expone el Gateway hacia afuera. Sin este transform, `Meals.Api` recibiría `/meals/platos/3` y fallaría porque no tiene ninguna ruta registrada con ese prefijo.
- Los `Destinations` apuntan a los puertos HTTP locales definidos en el `launchSettings.json` de cada servicio (`5094` para Meals.Api, `5062` para Planner.Api). Cuando se dockericen, estas direcciones cambiarán a los nombres de servicio de Docker (igual que pasó con la connection string de Postgres).

En `Program.cs` del Gateway, dos líneas activan todo esto:
```csharp
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy")); // lee la sección de arriba

app.MapReverseProxy(); // registra el middleware que intercepta y reenvía las peticiones
```

## 9. Estado de los secretos (pendiente de mejorar)

Ahora mismo la contraseña de Postgres vive en texto plano en dos lugares: `docker-compose.yml` del VPS y `appsettings.Development.json` de cada servicio local. Es aceptable para un proyecto de práctica que aún no está en un repositorio Git, pero **antes de inicializar git**, hay que:

- Agregar `appsettings.Development.json` (o al menos la sección `ConnectionStrings`) al `.gitignore`.
- Idealmente, mover la connection string local a `dotnet user-secrets` (un almacén fuera del proyecto, ligado a tu usuario de Windows) y la del VPS a un archivo `.env` con permisos restringidos, referenciado desde `docker-compose.yml` con `env_file`.

## 10. Qué falta (roadmap)

- [ ] Endpoints REST (controllers o minimal APIs) en `Meals.Api` para CRUD de `Plato`/`Ingrediente`.
- [ ] Endpoints REST en `Planner.Api` para `HistorialEntry`.
- [ ] Comunicación entre servicios (Planner → Meals) vía HTTP, típicamente con `HttpClient` tipado o un cliente generado.
- [ ] Dockerizar `Meals.Api`, `Planner.Api` y `ApiGateway`, y unirlos a la red `app-network` del VPS.
- [ ] Mover secretos a `user-secrets` / `.env` antes del primer commit a git.
- [ ] Actualizar `dotnet-ef` a la versión que coincide con el runtime (10.0.11) cuando se resuelva el error de reinstalación.
- [ ] Desplegar frontend en el mismo VPS.
