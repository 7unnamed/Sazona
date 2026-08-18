# Guía del proyecto ComidaDiaria — cómo se armó, desde cero

Esta guía explica, como si fuera una clase, todo lo que se hizo para levantar este backend: por qué cada pieza está donde está y qué problema resuelve. Sirve como referencia para reconstruir el proyecto o para entender las decisiones si vuelves dentro de unos meses.

## 1. Arquitectura general

La idea del proyecto es un **monolito modular pensado para evolucionar a microservicios**. Eso significa: hoy son servicios separados que corren de forma independiente (cada uno con su propia base de datos), pero conviven en una sola solución para simplificar el desarrollo mientras el proyecto es pequeño.

```
ComidaDiaria.slnx
├── src/Services/Meals/          → gestiona Platos e Ingredientes (Clean Architecture, 4 proyectos — sección 13)
│   ├── Meals.Domain/
│   ├── Meals.Application/
│   ├── Meals.Infrastructure/
│   └── Meals.Api/
├── src/Services/Planner.Api     → gestiona el historial de comidas planificadas (estructura ligera, por ahora)
└── src/Gateway/ApiGateway       → puerta de entrada única (YARP reverse proxy)
```

Reglas de diseño que se siguieron:

- **Cada servicio tiene su propia base de datos** (`meals_db`, `planner_db`). Esto es clave en microservicios: nunca comparten tablas directamente. Si `Planner.Api` necesita saber de un `Plato`, no hace un JOIN a la tabla de `Meals.Api` — guarda solo el `IdPlato` como referencia (ver sección 3).
- **EF Core + LINQ**, sin Dapper. Se prioriza productividad y migraciones automáticas sobre control fino de SQL.
- **Clean Architecture completa por servicio** (Domain / Application / Infrastructure / Api, en proyectos `.csproj` separados): se decidió invertir en esto desde el inicio porque el proyecto está pensado para crecer en serio, no como ejercicio de práctica descartable — ver el razonamiento completo y el porqué del cambio de criterio en la sección 13.
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
    "meals-cluster":   { "Destinations": { "meals-api":   { "Address": "http://meals-api:8080" } } },
    "planner-cluster":  { "Destinations": { "planner-api": { "Address": "http://planner-api:8080" } } }
  }
}
```

**Cómo leerlo:**
- `"Match": { "Path": "/meals/{**catch-all}" }` — cualquier URL que empiece con `/meals/` (el `{**catch-all}` captura el resto del path, sin importar cuántos segmentos tenga).
- `"Transforms": [ { "PathRemovePrefix": "/meals" } ]` — antes de reenviar la petición, le quita el prefijo `/meals` a la URL. Por ejemplo, una petición a `GATEWAY/meals/platos/3` llega a `Meals.Api` como `/platos/3` — el servicio interno no necesita saber que existe un prefijo `/meals`, eso es un detalle de cómo lo expone el Gateway hacia afuera. Sin este transform, `Meals.Api` recibiría `/meals/platos/3` y fallaría porque no tiene ninguna ruta registrada con ese prefijo.
- Los `Destinations` de arriba (`appsettings.json`, base/producción) apuntan a los **nombres de servicio de Docker** (`meals-api`, `planner-api`), resolubles por el DNS interno de la red `postgres_app-network` una vez que los tres contenedores están unidos a ella. En `appsettings.Development.json` (tu PC) se sobreescriben con `http://localhost:5094` y `http://localhost:5062`, los puertos HTTP locales definidos en el `launchSettings.json` de cada servicio — el mismo patrón de capas de configuración que las connection strings de Postgres (sección 5).

En `Program.cs` del Gateway, dos líneas activan todo esto:
```csharp
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy")); // lee la sección de arriba

app.MapReverseProxy(); // registra el middleware que intercepta y reenvía las peticiones
```

## 9. Manejo de secretos

Ya se subió el código a Git, así que los secretos se separaron del repo:

- **`.gitignore`** excluye `appsettings.Development.json` de `Meals.Api` y `Planner.Api` (contienen la password de Postgres en texto plano para tu PC), `.env`, `*.pem` y `.claude/settings.local.json`. En su lugar, el repo trae un `appsettings.Development.json.example` de cada servicio como plantilla — al clonar el proyecto en una PC nueva, hay que copiarlo sin el `.example` y completar la password real.
- **`appsettings.json`** (base, el que sí se commitea) ya **no** trae ninguna password — solo `Host=postgres;...;Username=comidiaria_admin`, sin el campo `Password`.
- La password real se inyecta en runtime vía la variable de entorno `POSTGRES_PASSWORD`. En `Program.cs` de `Meals.Api` y `Planner.Api`:
  ```csharp
  var postgresPassword = builder.Configuration["POSTGRES_PASSWORD"];
  if (!string.IsNullOrEmpty(postgresPassword))
  {
      var csBuilder = new NpgsqlConnectionStringBuilder(connectionString) { Password = postgresPassword };
      connectionString = csBuilder.ConnectionString;
  }
  ```
  `NpgsqlConnectionStringBuilder` toma la connection string base (sin password) y le agrega la password que llega por variable de entorno, sin tener que mantener dos strings completas por ambiente.
- En el VPS, esa variable vive en `~/comidiaria/backend-net/.env` (permisos `600`, fuera de git), y `docker-compose.yml` la lee automáticamente porque Docker Compose carga `.env` del mismo directorio por convención:
  ```yaml
  environment:
    POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
  ```

Pendiente para más adelante (no bloqueante hoy): mover esto a un gestor de secretos real (Docker Secrets, Vault, etc.) si el proyecto crece más allá de práctica personal.

## 10. CI/CD con GitHub Actions — despliegue por tags

Se decidió replicar un flujo típico de GitLab: **el despliegue no se dispara con cada push a `main`, sino al crear y empujar un tag**. Esto separa "guardar avances" de "publicar una versión" — puedes commitear y subir a `main` todo el día sin tocar producción, y solo cuando un conjunto de cambios está listo, lo "marcas" con un tag y eso dispara el deploy.

### 10.1 El repositorio

Se inicializó git en el proyecto (`git init`), se armó un `.gitignore` (sección 9) y se creó el repo remoto en GitHub: [`7unnamed/Sazona`](https://github.com/7unnamed/Sazona). El primer commit se subió a la rama `main`.

### 10.2 Dockerfiles de cada servicio

Cada servicio (`Meals.Api`, `Planner.Api`, `ApiGateway`) tiene su propio `Dockerfile` con **build multi-stage**: una etapa `build` con el SDK completo de .NET (pesado, con compilador) que solo se usa para compilar, y una etapa `final` con la imagen runtime `aspnet` (mucho más liviana, sin herramientas de build) que es la que realmente corre en producción.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Meals.Api/Meals.Api.csproj", "src/Services/Meals.Api/"]
RUN dotnet restore "src/Services/Meals.Api/Meals.Api.csproj"
COPY src/Services/Meals.Api/ src/Services/Meals.Api/
WORKDIR /src/src/Services/Meals.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Meals.Api.dll"]
```

Detalle importante: se copia primero solo el `.csproj` y se hace `dotnet restore` **antes** de copiar el resto del código. Docker cachea cada instrucción (`RUN`, `COPY`) por capas — si solo cambia código C# pero no las dependencias del `.csproj`, Docker reutiliza la capa del `restore` (que es la más lenta, porque descarga paquetes NuGet) en vez de repetirla en cada build.

También se fija `ASPNETCORE_URLS=http://+:8080` explícitamente: las imágenes oficiales de ASP.NET Core desde .NET 8 en adelante escuchan por defecto en el puerto 8080 dentro del contenedor, y se declara con `EXPOSE 8080` para que quede documentado (aunque `EXPOSE` no publica el puerto por sí solo, eso lo hace `docker-compose.yml`).

### 10.3 `docker-compose.yml` de despliegue (raíz del repo)

Este archivo es distinto al de Postgres (sección 7) — orquesta los **3 servicios de la aplicación**, no la base de datos:

```yaml
services:
  meals-api:
    build:
      context: .
      dockerfile: src/Services/Meals.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    networks:
      - postgres_app-network
  # ...planner-api es igual...

  api-gateway:
    build:
      context: .
      dockerfile: src/Gateway/ApiGateway/Dockerfile
    ports:
      - "80:8080"
    depends_on: [meals-api, planner-api]
    networks:
      - postgres_app-network

networks:
  postgres_app-network:
    external: true
```

Dos decisiones clave:
- **`networks: postgres_app-network: external: true`** — en vez de crear una red nueva, este compose se **une** a la red que ya creó el `docker-compose.yml` de Postgres (sección 7.1). Docker Compose nombra sus redes como `<carpeta-del-compose>_<nombre-en-el-yml>`; como el compose de Postgres vive en `~/comidiaria/postgres/`, su red se llamó `postgres_app-network` — por eso el nombre no es arbitrario, se verificó con `docker network ls` antes de escribirlo.
- **Solo `api-gateway` publica un puerto** (`80:8080`, HTTP público). `meals-api` y `planner-api` **no publican ningún puerto al host** — solo son alcanzables dentro de la red Docker, por su nombre de servicio. Esto refuerza la idea de que el Gateway es el único punto de entrada; nadie desde internet puede pegarle directo a `Meals.Api` saltándose el Gateway.

### 10.4 El workflow de GitHub Actions

Archivo `.github/workflows/deploy.yml`:

```yaml
name: Deploy to VPS

on:
  push:
    tags:
      - 'v*'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy over SSH
        uses: appleboy/ssh-action@v1.0.3
        with:
          host: ${{ secrets.VPS_HOST }}
          username: ${{ secrets.VPS_USER }}
          key: ${{ secrets.VPS_SSH_KEY }}
          script: |
            cd ~/comidiaria/backend-net
            git fetch --tags --force
            git checkout ${{ github.ref_name }}
            docker compose build
            docker compose up -d
            docker image prune -f
```

**Cómo leerlo:**
- `on: push: tags: ['v*']` — el workflow **solo** se dispara cuando se empuja un tag cuyo nombre empieza con `v` (`v0.1.0`, `v1.2.3`, etc.). Un push normal a `main` no activa nada.
- `appleboy/ssh-action` es una Action de la comunidad que abre una conexión SSH y corre el `script` indicado en el servidor remoto — es el reemplazo directo de lo que en GitLab CI harías con un `before_script` + `ssh` manual.
- El `script` **no compila nada en GitHub** (se eligió esta estrategia — más simple, sin necesidad de un registry de imágenes — a costa de usar CPU/RAM del propio VPS para el build): se conecta al VPS, trae el tag exacto que se acaba de crear (`git fetch --tags` + `git checkout <tag>`, donde `${{ github.ref_name }}` es una variable que GitHub Actions llena automáticamente con el nombre del tag que disparó el workflow), reconstruye las imágenes con ese código y reinicia los contenedores.
- `docker image prune -f` al final borra las imágenes Docker "huérfanas" (versiones anteriores que ya nadie referencia) para no llenar el disco del VPS con cada deploy.

### 10.5 Secrets del workflow

El workflow necesita credenciales para conectarse por SSH. Se guardaron como **GitHub Secrets** (Settings → Secrets and variables → Actions), nunca en el código:

| Secret | Valor |
|---|---|
| `VPS_HOST` | La IP del VPS |
| `VPS_USER` | `azureuser` |
| `VPS_SSH_KEY` | Una llave privada SSH **dedicada solo a este propósito** |

Sobre la llave: en vez de usar la llave `.pem` personal (la que usas para administrar todo el VPS), se generó un par de llaves nuevo exclusivo para el deploy (`ssh-keygen -t ed25519`), y solo se agregó la **llave pública** a `~/.ssh/authorized_keys` del VPS. La privada es la que vive en el secret de GitHub. La ventaja: si esa llave se filtrara algún día, se revoca borrando esa única línea de `authorized_keys`, sin tener que rotar tu acceso administrativo principal al servidor.

### 10.6 El flujo de trabajo día a día

```bash
# 1. Trabajas normal, commiteas y subes a main (no dispara deploy)
git add .
git commit -m "agrego endpoint de platos"
git push origin main

# 2. Cuando quieres publicar esa versión en el VPS:
git tag v0.2.0
git push origin v0.2.0   # esto SÍ dispara el workflow
```

Puedes revisar el progreso y el resultado de cada deploy en la pestaña **Actions** del repo en GitHub.

## 11. Endpoints REST de `Meals.Api`

Se usaron **Minimal APIs** (no controllers) para mantener consistencia con el estilo que ya traía el proyecto (`Program.cs` con `app.MapGet(...)`). Para que `Program.cs` no se llenara de código, cada grupo de endpoints se separó en su propio archivo con un método de extensión:

```csharp
// Endpoints/PlatoEndpoints.cs
public static class PlatoEndpoints
{
    public static void MapPlatoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/platos").WithTags("Platos");
        group.MapGet("/", ...);
        // ...
    }
}

// Program.cs
app.MapPlatoEndpoints();
app.MapIngredienteEndpoints();
```

`app.MapGroup("/platos")` agrupa todas las rutas bajo un prefijo común y permite darles metadata compartida (como `WithTags` para que aparezcan agrupadas en la documentación OpenAPI).

### 11.1 Contratos (DTOs) en vez de exponer las entidades

Los endpoints **no** reciben ni devuelven directamente `Plato` o `Ingrediente` (las clases de `Domain/`). En su lugar, se creó una carpeta `Contracts/` con records específicos para la API:

```csharp
public record CrearPlatoRequest(string NombrePlato, TipoComida TipoComida, int PorcionesBase, List<IngredienteRequest> Ingredientes);
public record PlatoResponse(int IdPlato, string NombrePlato, TipoComida TipoComida, int PorcionesBase, List<IngredienteResponse> Ingredientes);
```

¿Por qué no reusar la entidad directamente? Porque la entidad de dominio y el "contrato" con el mundo exterior tienen ciclos de vida distintos: si mañana `Plato` gana una propiedad interna (por ejemplo, un campo de auditoría), no queremos que aparezca automáticamente en la respuesta JSON de la API sin que sea una decisión explícita. También evita el problema de "over-posting" (que alguien mande en el body un campo que no debería poder setear, como `IdPlato` en un create).

### 11.2 Rutas disponibles

| Método | Ruta | Qué hace |
|---|---|---|
| `GET` | `/platos` | Lista todos los platos con sus ingredientes |
| `GET` | `/platos/{idPlato}` | Un plato puntual (404 si no existe) |
| `POST` | `/platos` | Crea un plato, opcionalmente con sus ingredientes en el mismo request |
| `PUT` | `/platos/{idPlato}` | Actualiza los datos del plato (no toca sus ingredientes) |
| `DELETE` | `/platos/{idPlato}` | Borra el plato — por el `OnDelete(DeleteBehavior.Cascade)` de la sección 3, sus ingredientes se borran solos |
| `POST` | `/platos/{idPlato}/ingredientes` | Agrega un ingrediente a un plato existente |
| `PUT` | `/platos/{idPlato}/ingredientes/{idIngrediente}` | Edita un ingrediente puntual |
| `DELETE` | `/platos/{idPlato}/ingredientes/{idIngrediente}` | Borra un ingrediente puntual |

Los endpoints de ingredientes van **anidados bajo `/platos/{idPlato}/ingredientes`** (no `/ingredientes` a secas) porque un ingrediente no tiene sentido fuera del contexto de su plato — refleja la relación de dominio en la forma de la URL.

Se probó el flujo completo (crear con ingredientes anidados, listar, editar, agregar/borrar ingrediente suelto, 404 en ids inexistentes) contra la base real del VPS a través del túnel SSH, y quedó funcionando correctamente.

## 13. Migración a Clean Architecture completa (`Meals`)

### 13.1 Por qué se cambió de criterio

Hasta la sección 12, `Meals.Api` era **un solo proyecto** con carpetas internas (`Domain/`, `Data/`, `Contracts/`, `Endpoints/`). Esa estructura ligera es la recomendación correcta cuando no hay certeza de que el proyecto vaya a crecer — evita cargar con capas que no protegen nada real todavía (ver razonamiento original en la sección 3).

Pero acá el plan es explícito: este proyecto va a crecer en serio. Con esa restricción confirmada, el cálculo cambia — el costo de armar la separación completa **ahora**, con 2 entidades, es bajo; migrar más adelante con 20 entidades y lógica de negocio real ya enredada sería mucho más caro. Por eso se migró `Meals` a **4 proyectos `.csproj` separados**, uno por capa, con **referencias unidireccionales que el compilador obliga a respetar**:

```
Meals.Domain          (sin dependencias — solo entidades y enums)
    ↑
Meals.Application     (DTOs, interfaces de repositorio/servicio, lógica de negocio)
    ↑
Meals.Infrastructure  (EF Core: DbContext, implementación del repositorio, migraciones)
    ↑
Meals.Api             (ASP.NET Core: Program.cs, endpoints, inyección de dependencias)
```

La flecha va de abajo hacia arriba porque así apuntan las `ProjectReference`: `Meals.Api` referencia a `Meals.Application` y `Meals.Infrastructure`; `Meals.Infrastructure` referencia a `Meals.Application`; `Meals.Application` referencia a `Meals.Domain`; y `Meals.Domain` no referencia a nada. Esto **no es solo organización visual** — si en `Meals.Domain` alguien intentara escribir `using Microsoft.EntityFrameworkCore`, el build fallaría, porque ese proyecto no tiene esa referencia. El compilador impide físicamente que la capa de negocio dependa de detalles de infraestructura.

### 13.2 Qué hace cada capa ahora

- **`Meals.Domain`** — exactamente lo que ya había en `Domain/`: `Plato`, `Ingrediente`, `TipoComida`. Cero paquetes NuGet, cero dependencias externas. Es la capa más estable del proyecto — cambia poco y todo lo demás depende de ella.

- **`Meals.Application`** — creció respecto a lo que había en `Contracts/`. Ahora tiene tres carpetas:
  - `Contracts/` — los mismos DTOs de antes (`CrearPlatoRequest`, `PlatoResponse`, etc., sección 11.1).
  - `Interfaces/` — los **contratos entre capas**: `IPlatoRepository` (lo que la capa de negocio espera de la persistencia, sin saber que es EF Core o Postgres) e `IPlatoService` (lo que los endpoints esperan de la lógica de negocio).
  - `Services/PlatoService.cs` — la lógica de negocio real: recibe un `IPlatoRepository` por constructor (nunca un `DbContext` directo) y traduce entre `Contracts` y `Domain`. Antes esta lógica vivía mezclada dentro de los propios endpoints (sección 11); ahora los endpoints son una capa delgada que solo llama al servicio.

  ```csharp
  public class PlatoService : IPlatoService
  {
      private readonly IPlatoRepository _platoRepository; // interfaz, no la clase concreta

      public PlatoService(IPlatoRepository platoRepository) => _platoRepository = platoRepository;

      public async Task<PlatoResponse> CreateAsync(CrearPlatoRequest request, CancellationToken ct = default)
      {
          var plato = new Plato { NombrePlato = request.NombrePlato, /* ... */ };
          _platoRepository.Add(plato);
          await _platoRepository.SaveChangesAsync(ct);
          return ToResponse(plato);
      }
  }
  ```

  Que `PlatoService` dependa de `IPlatoRepository` (interfaz) y no de `PlatoRepository` (clase con EF Core) es la esencia del patrón: si mañana cambia el motor de persistencia, `PlatoService` no se entera — solo cambia qué implementación de la interfaz se registra en `Program.cs`.

- **`Meals.Infrastructure`** — lo que antes era `Data/` y `Migrations/`, más algo nuevo: `Repositories/PlatoRepository.cs`, la implementación real de `IPlatoRepository` con EF Core:

  ```csharp
  public class PlatoRepository : IPlatoRepository
  {
      private readonly MealsDbContext _dbContext;
      public PlatoRepository(MealsDbContext dbContext) => _dbContext = dbContext;

      public async Task<Plato?> GetByIdAsync(int idPlato, CancellationToken ct = default) =>
          await _dbContext.Platos.Include(p => p.Ingredientes).FirstOrDefaultAsync(p => p.IdPlato == idPlato, ct);
      // ...
  }
  ```

  Nota de diseño: `IPlatoRepository` **no tiene métodos separados para `Ingrediente`** — `Ingrediente` no existe sin un `Plato` (es un *aggregate* en términos de DDD: el `Plato` es la raíz, y `Ingrediente` solo se modifica a través de él). Por eso `PlatoService.AddIngredienteAsync` trae el `Plato` completo (con sus ingredientes incluidos vía `Include`), modifica la colección en memoria (`plato.Ingredientes.Add(...)`) y guarda — EF Core detecta el cambio en la colección rastreada y genera el `INSERT`/`DELETE` correspondiente solo. Es el mismo resultado que antes escribiendo directo `db.Ingredientes.Add(...)`, pero ahora la decisión de "cómo se persiste" vive encapsulada en el repositorio, no dispersa en cada endpoint.

- **`Meals.Api`** — quedó como la capa más delgada: `Program.cs` arma la inyección de dependencias, y los `Endpoints/` ya no tocan EF Core en absoluto, solo `IPlatoService`:

  ```csharp
  group.MapPost("/", async (CrearPlatoRequest request, IPlatoService platoService) =>
  {
      var plato = await platoService.CreateAsync(request);
      return Results.Created($"/platos/{plato.IdPlato}", plato);
  });
  ```

  Nota cómo Minimal API **inyecta `IPlatoService` directo como parámetro del handler** — no hace falta pedirlo manualmente, ASP.NET Core lo resuelve del contenedor de DI automáticamente porque el tipo está registrado en `Program.cs`:

  ```csharp
  builder.Services.AddScoped<IPlatoRepository, PlatoRepository>();
  builder.Services.AddScoped<IPlatoService, PlatoService>();
  ```

  `AddScoped` significa "una instancia nueva por cada request HTTP" — el ciclo de vida correcto para algo que envuelve un `DbContext` (que tampoco se debe compartir entre requests).

### 13.3 El bug real que apareció al separar en proyectos: conflicto de versiones de EF Core

Al mover el código a 4 proyectos, la primera compilación falló con este error:

```
error CS1705: El ensamblado 'Meals.Infrastructure' [...] usa 'Microsoft.EntityFrameworkCore, Version=10.0.11.0' [...]
que tiene una versión superior a la del ensamblado [...] con la identidad 'Microsoft.EntityFrameworkCore, Version=10.0.4.0'
```

**Causa raíz:** cuando todo vivía en un solo `.csproj`, NuGet resolvía todas las versiones de paquetes en un único proceso y listo. Al separar en proyectos, cada `.csproj` resuelve sus propias versiones de forma independiente:

- `Meals.Infrastructure` referencia `Npgsql.EntityFrameworkCore.PostgreSQL` (que internamente pide `Microsoft.EntityFrameworkCore.Relational ~10.0.4`) **y también** `Microsoft.EntityFrameworkCore.Design 10.0.11` — dentro de ese mismo proyecto, NuGet ve ambos pedidos y elige la versión más alta (10.0.11) para todo.
- `Meals.Api` no referencia Npgsql para nada (no debería — ni sabe que existe Postgres, esa es la gracia de la capa de Infraestructura), así que su propia resolución de paquetes nunca ve la necesidad de subir a 10.0.11, y se queda en 10.0.4 por defecto.
- Resultado: el `.dll` compilado de `Meals.Infrastructure` (que `Meals.Api` consume vía `ProjectReference`) fue compilado contra 10.0.11, pero `Meals.Api` en su propio build intenta cargarlo esperando 10.0.4. Choque de versiones.

**La solución: Central Package Management (CPM).** Se creó un archivo `Directory.Packages.props` en la raíz del repo — MSBuild lo detecta automáticamente en cualquier proyecto de cualquier subcarpeta, sin necesidad de referenciarlo:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.11" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.11" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.11" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
    <!-- ... -->
  </ItemGroup>
</Project>
```

Con esto, **ningún `.csproj` de la solución vuelve a escribir un número de versión** — solo declara qué paquete usa, y la versión se toma de este archivo único:

```xml
<!-- antes -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.11" />

<!-- ahora -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
```

Dos detalles que costó encontrar y vale la pena recordar:

1. **`ManagePackageVersionsCentrally`** por sí solo solo fija la versión de los paquetes que un proyecto referencia **directamente**. No alcanza para este bug, porque `Meals.Api` nunca referencia Npgsql directamente — lo recibe *transitivamente* a través de la `ProjectReference` a `Meals.Infrastructure`.
2. **`CentralPackageTransitivePinningEnabled`** es la propiedad que faltaba: obliga a que las versiones fijadas en `Directory.Packages.props` se apliquen también a paquetes que llegan de forma transitiva (a través de otro paquete o de otro proyecto), no solo a las referencias directas. Sin esta línea, el conflicto seguía apareciendo incluso después de agregar CPM.

Esto es exactamente el tipo de problema — silencioso mientras el proyecto es un monolito de un solo proyecto, y que aparece de golpe al separar en capas — que justifica invertir en `Directory.Packages.props` desde ahora: a medida que se agreguen más proyectos (la migración de `Planner.Api` a esta misma estructura, por ejemplo), todos comparten automáticamente las mismas versiones sin volver a pisar este bug.

### 13.4 Impacto en Dockerfile y solución

- **`ComidaDiaria.slnx`** ahora lista los 4 proyectos de `Meals` en vez de uno solo.
- **`src/Services/Meals/Meals.Api/Dockerfile`** se actualizó para copiar y restaurar los 4 `.csproj` (en orden, antes de copiar el resto del código, por la misma razón de cacheo de capas explicada en la sección 10.2) antes de publicar:
  ```dockerfile
  COPY ["src/Services/Meals/Meals.Domain/Meals.Domain.csproj", "src/Services/Meals/Meals.Domain/"]
  COPY ["src/Services/Meals/Meals.Application/Meals.Application.csproj", "src/Services/Meals/Meals.Application/"]
  COPY ["src/Services/Meals/Meals.Infrastructure/Meals.Infrastructure.csproj", "src/Services/Meals/Meals.Infrastructure/"]
  COPY ["src/Services/Meals/Meals.Api/Meals.Api.csproj", "src/Services/Meals/Meals.Api/"]
  RUN dotnet restore "src/Services/Meals/Meals.Api/Meals.Api.csproj"
  ```
- **`docker-compose.yml`** (raíz) se actualizó apuntando al nuevo path del Dockerfile (`src/Services/Meals/Meals.Api/Dockerfile`).
- Se verificó el flujo completo (crear con ingredientes anidados, listar, obtener por id, agregar ingrediente, borrar) contra la base real del VPS a través del túnel SSH — el comportamiento externo de la API es idéntico al de antes de la migración, como debe ser: refactorizar capas no debería cambiar el contrato de la API.

### 13.5 Migración de `Planner.Api`

Con el patrón de Meals ya probado, se migró `Planner` al mismo esquema de 4 proyectos (`Planner.Domain` / `Planner.Application` / `Planner.Infrastructure` / `Planner.Api`), con dos diferencias respecto a Meals que vale la pena señalar:

- **`Planner.Api` no tenía endpoints reales todavía** — `Program.cs` seguía siendo el scaffold sin tocar (`/weatherforecast`), sin capa de servicio ni repositorio. A diferencia de Meals (donde se movió lógica ya existente de los endpoints hacia el servicio), acá `IHistorialEntryRepository`, `IHistorialEntryService`, `HistorialEntryService`, `HistorialEntryRepository` y los endpoints de `/historial` se **crearon de cero**, siguiendo el mismo molde CRUD que `Plato` (Get all / Get by id / Create / Update / Delete).
- **`HistorialEntry` no tiene una colección anidada** como `Plato` tiene `Ingredientes`. Por eso `IHistorialEntryRepository` es más simple que `IPlatoRepository`: no hace falta ningún `.Include(...)` porque no hay hijos que cargar, y no existe el patrón de "traer el agregado completo para modificar un hijo en memoria" de la sección 13.2. `IdPlato` sigue siendo una referencia suelta a otro microservicio (sin FK ni navegación — ver sección 3), y eso no cambió con la migración.

**Sobre las migraciones de EF Core**: se hizo el mismo hand-edit que en Meals — cambiar `namespace`, el `using` y el atributo `[DbContext(typeof(...))]` en los 3 archivos de `Migrations/`, sin borrar ni regenerar nada. Esto es obligatorio, no opcional: la migración `InitialCreate` de Planner **ya estaba aplicada** contra `planner_db` en el VPS antes de este refactor (columna registrada en `__EFMigrationsHistory` con ese Id exacto). Si se hubiera borrado y regenerado la migración, EF Core la vería como una migración nueva y distinta, y el próximo `dotnet ef database update` intentaría crear las tablas de nuevo (fallando, porque ya existen) en vez de reconocer que no hay nada pendiente.

Se verificó el CRUD completo de `/historial` (crear, listar, obtener por id, actualizar, borrar, 404 tras el borrado) contra `planner_db` real en el VPS, igual que se hizo con Meals.

**`ApiGateway` quedó sin migrar** — hoy es solo configuración declarativa de YARP (rutas + clusters en `appsettings.json`), sin lógica propia; probablemente nunca necesite las 4 capas, a menos que en el futuro gane responsabilidades como autenticación o rate limiting con código real detrás.

## 14. Qué falta (roadmap)

- [x] Dockerizar `Meals.Api`, `Planner.Api` y `ApiGateway`, y unirlos a la red `postgres_app-network` del VPS.
- [x] Repositorio en GitHub + pipeline de CI/CD por tags.
- [x] Endpoints REST (Minimal APIs) en `Meals.Api` para CRUD de `Plato`/`Ingrediente`.
- [x] Migrar `Meals` a Clean Architecture completa (Domain/Application/Infrastructure/Api) + Central Package Management.
- [x] Migrar `Planner.Api` a la misma estructura de 4 proyectos, con CRUD real de `HistorialEntry`.
- [x] Autenticación JWT (`Auth` service) + auditoría automática (usuario/fecha en creación, actualización y soft-delete) en todos los servicios — ver sección 16.
- [x] Logging estructurado con Serilog (consola + archivo rotativo) en los 4 servicios, con `UserId` enriquecido desde el JWT — ver sección 17.
- [ ] Migrar `ApiGateway` si en algún momento gana lógica propia (hoy es solo config de YARP, probablemente no lo necesite nunca).
- [ ] Aplicar la convención documentada en la Sección 15 a cualquier servicio nuevo (incluyendo `IAuditableEntity`/`ISoftDeletable` y `RequireAuthorization()` desde el día uno — ver sección 16.6).
- [ ] Comunicación entre servicios (Planner → Meals) vía HTTP, típicamente con `HttpClient` tipado o un cliente generado.
- [ ] Endpoint para elevar un usuario a rol `Administrador` (hoy solo se puede editar directo en la base) y usar los roles para restringir endpoints (hoy el rol viaja en el JWT pero no se usa para nada todavía).
- [ ] Refresh tokens (hoy el JWT expira a los 60 minutos y no hay forma de renovarlo sin volver a loguearse).
- [ ] Actualizar `dotnet-ef` a la versión que coincide con el runtime (10.0.11) cuando se resuelva el error de reinstalación.
- [ ] Desplegar frontend en el mismo VPS.
- [ ] Configurar HTTPS en el Gateway (hoy el puerto 80 es HTTP plano) — típicamente con un reverse proxy adicional tipo Caddy/Nginx + Let's Encrypt, o Kestrel con certificado si el Gateway queda expuesto directo.
- [x] Verificado en el deploy real: `~/comidiaria/backend-net/logs/<servicio>/` se creó sin problemas de permisos y los 4 servicios escriben sus logs correctamente (sección 17.4).

## 15. Convención de estructura para nuevos servicios

Con `Meals` y `Planner` ya migrados, esta es la receta fija para **cualquier servicio nuevo** que se agregue al proyecto (`ServiceName` = el nombre del servicio, ej. `Users`, `Notifications`, etc.). No hay que re-derivar el patrón cada vez — seguir esta lista en orden.

### 15.1 Esqueleto de carpetas

```
src/Services/ServiceName/
├── ServiceName.Domain/
├── ServiceName.Application/
├── ServiceName.Infrastructure/
└── ServiceName.Api/
```

### 15.2 Reglas de referencias por proyecto (unidireccionales, de abajo hacia arriba)

| Proyecto | SDK | PackageReference | ProjectReference |
|---|---|---|---|
| `ServiceName.Domain` | `Microsoft.NET.Sdk` | ninguno | ninguno |
| `ServiceName.Application` | `Microsoft.NET.Sdk` | ninguno | `..\ServiceName.Domain` |
| `ServiceName.Infrastructure` | `Microsoft.NET.Sdk` | `Microsoft.EntityFrameworkCore.Design` (PrivateAssets=all) + `Npgsql.EntityFrameworkCore.PostgreSQL` | `..\ServiceName.Application` |
| `ServiceName.Api` | `Microsoft.NET.Sdk.Web` | `Microsoft.AspNetCore.OpenApi` + **`Microsoft.EntityFrameworkCore`** (directo) | `..\ServiceName.Application` + `..\ServiceName.Infrastructure` |

La referencia directa a `Microsoft.EntityFrameworkCore` en `ServiceName.Api` **no es opcional** — es la que evita desde el día uno el conflicto de versiones documentado en la sección 13.3 (`Meals.Api` lo necesitó para poder llamar `AddDbContext`, y sin ella el build falla o queda con un warning de versiones desalineadas apenas el servicio tenga más de un proyecto).

**Ningún `PackageReference` de ningún proyecto lleva `Version="..."`** — las versiones viven únicamente en `Directory.Packages.props` (raíz del repo). Si el servicio nuevo necesita un paquete que todavía no está en ese archivo, se agrega ahí como `<PackageVersion Include="..." Version="..." />` una sola vez, nunca en el `.csproj` del proyecto.

### 15.3 Convención de namespaces

| Carpeta | Namespace |
|---|---|
| `ServiceName.Domain/` (raíz) | `ServiceName.Domain` |
| `ServiceName.Domain/Enums/` | `ServiceName.Domain.Enums` |
| `ServiceName.Application/Contracts/` | `ServiceName.Application.Contracts` |
| `ServiceName.Application/Interfaces/` | `ServiceName.Application.Interfaces` |
| `ServiceName.Application/Services/` | `ServiceName.Application.Services` |
| `ServiceName.Infrastructure/Data/` | `ServiceName.Infrastructure.Data` |
| `ServiceName.Infrastructure/Repositories/` | `ServiceName.Infrastructure.Repositories` |
| `ServiceName.Infrastructure/Migrations/` | `ServiceName.Infrastructure.Migrations` |
| `ServiceName.Api/Endpoints/` | `ServiceName.Api.Endpoints` |

### 15.4 Patrón por capa (qué va en cada una)

1. **Domain**: entidades planas (clases con propiedades) y enums. Nombres de propiedades descriptivos (`IdEntidad`, no `Id` — sección 1). Cero lógica, cero atributos de EF Core (eso vive en `OnModelCreating`, no en la entidad).
2. **Application**: por cada entidad "raíz" (aggregate root, en términos DDD — la que se expone por la API), un trío `IEntidadRepository` / `IEntidadService` / `EntidadService`, más los DTOs en `Contracts/` (`CrearEntidadRequest`, `ActualizarEntidadRequest`, `EntidadResponse`). El `Service` mapea a mano entre `Domain` y `Contracts` — no hay AutoMapper en este proyecto, es deliberado para mantener el mapeo explícito y fácil de debuggear.
3. **Infrastructure**: `Data/ServiceNameDbContext.cs` (hereda `DbContext`, configura keys/constraints en `OnModelCreating`) + `Repositories/EntidadRepository.cs` (implementa la interfaz de Application usando el `DbContext` directo). Si la entidad tiene hijos anidados (como `Plato`/`Ingrediente`), el repositorio expone al padre con `.Include(...)` y el `Service` manipula la colección en memoria (sección 13.2) — si no los tiene (como `HistorialEntry`), el repositorio es CRUD plano sin `Include`.
4. **Api**: `Program.cs` registra `AddDbContext`, `AddScoped<IEntidadRepository, EntidadRepository>()`, `AddScoped<IEntidadService, EntidadService>()`, y llama a `app.MapEntidadEndpoints()`. Los endpoints en `Endpoints/` son Minimal API agrupados con `MapGroup`, inyectan la interfaz de servicio directo como parámetro del handler, y nunca importan EF Core.

### 15.5 Plantilla de `Program.cs`

```csharp
using ServiceName.Api.Endpoints;
using ServiceName.Application.Interfaces;
using ServiceName.Application.Services;
using ServiceName.Infrastructure.Data;
using ServiceName.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.UseSharedSerilog("ServiceName.Api"); // sección 17 — logging estructurado, antes que cualquier otro AddX

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("ServiceNameDb");
var postgresPassword = builder.Configuration["POSTGRES_PASSWORD"];
if (!string.IsNullOrEmpty(postgresPassword))
{
    var csBuilder = new NpgsqlConnectionStringBuilder(connectionString) { Password = postgresPassword };
    connectionString = csBuilder.ConnectionString;
}

builder.Services.AddDbContext<ServiceNameDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IEntidadRepository, EntidadRepository>();
builder.Services.AddScoped<IEntidadService, EntidadService>();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.MapOpenApi(); }
app.UseSharedRequestLogging(); // sección 17
app.UseHttpsRedirection();
app.MapEntidadEndpoints();
app.Run();
```

Este patrón de connection string (base sin password en `appsettings.json`, password inyectada por la variable de entorno `POSTGRES_PASSWORD`) está explicado en la sección 9 — se replica igual para todo servicio nuevo.

### 15.6 Plantilla de Dockerfile (multi-stage, cachea el `restore`)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/ServiceName/ServiceName.Domain/ServiceName.Domain.csproj", "src/Services/ServiceName/ServiceName.Domain/"]
COPY ["src/Services/ServiceName/ServiceName.Application/ServiceName.Application.csproj", "src/Services/ServiceName/ServiceName.Application/"]
COPY ["src/Services/ServiceName/ServiceName.Infrastructure/ServiceName.Infrastructure.csproj", "src/Services/ServiceName/ServiceName.Infrastructure/"]
COPY ["src/Services/ServiceName/ServiceName.Api/ServiceName.Api.csproj", "src/Services/ServiceName/ServiceName.Api/"]
RUN dotnet restore "src/Services/ServiceName/ServiceName.Api/ServiceName.Api.csproj"
COPY src/Services/ServiceName/ src/Services/ServiceName/
WORKDIR /src/src/Services/ServiceName/ServiceName.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ServiceName.Api.dll"]
```

Copiar cada `.csproj` individualmente **antes** del código completo no es cosmético: así Docker cachea la capa del `restore` (la más lenta, descarga paquetes NuGet) y solo la repite cuando cambia algún `.csproj`, no en cada cambio de código C# (sección 10.2).

### 15.7 Registro en archivos raíz

1. **`ComidaDiaria.slnx`**: agregar un `<Folder Name="/src/Services/ServiceName/">` con los 4 `<Project Path="..." />`, mismo estilo que los folders de Meals/Planner.
2. **`docker-compose.yml`**: agregar un servicio nuevo (`servicename-api`) apuntando a `src/Services/ServiceName/ServiceName.Api/Dockerfile`, unido a `postgres_app-network`, con `POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}` si usa Postgres.
3. Si el servicio necesita su propia base de datos: agregarla al script `init-databases.sh` del VPS (sección 7.1) — `CREATE DATABASE servicename_db;`.
4. Si debe ser accesible desde el Gateway: agregar su `Route`/`Cluster` en `ApiGateway/appsettings.json` (sección 8), apuntando por nombre de servicio Docker (`http://servicename-api:8080`) en la config base y `http://localhost:PUERTO` en `appsettings.Development.json`.

### 15.8 Regla para migraciones ya aplicadas

Si en algún momento se renombra el namespace de un servicio cuya migración **ya corrió** contra una base de datos real (VPS o cualquier ambiente compartido), el cambio se hace **a mano** sobre los 3 archivos existentes en `Migrations/` (namespace, `using`, atributo `[DbContext(typeof(...))]`, y los strings literales con el nombre completo de la entidad) — nunca borrando y regenerando con `dotnet ef migrations add`. Regenerar crearía una migración con un Id distinto que EF Core no reconciliaría con la fila ya existente en `__EFMigrationsHistory`, y el próximo `database update` fallaría intentando recrear tablas que ya existen (sección 13.5).

## 16. Autenticación JWT + auditoría automática

Hasta acá, cualquiera podía pegarle a la API sin identificarse, y nadie sabía quién había creado, editado o borrado un registro. Esta sección agrega un servicio de login (`Auth`) que emite JWT, y un mecanismo **automático** de auditoría que se activa en `Meals`, `Planner` y cualquier servicio futuro sin escribir código de auditoría en cada uno.

### 16.1 `BuildingBlocks` — la única excepción al aislamiento entre servicios

Hasta ahora, cada servicio (`Meals`, `Planner`) era una isla total: ni siquiera compartían el enum `TipoComida`, que está duplicado en cada uno a propósito (sección 1). Pero pedir "auditoría automática en todos los servicios" sin repetir el mismo `SaveChangesAsync` línea por línea en cada `DbContext` requiere **un lugar único** donde vivir esa lógica. Por eso se creó `src/BuildingBlocks/`, con la misma idea de capas que un servicio normal pero sin `Api` (no expone HTTP, solo código para que otros proyectos lo referencien):

```
BuildingBlocks.Domain          → interfaces IAuditableEntity / ISoftDeletable (sin dependencias)
BuildingBlocks.Application     → interfaz ICurrentUserService
BuildingBlocks.Infrastructure  → CurrentUserService, AuditableDbContext, extensión de JWT
```

La regla que se mantiene: **esto es infraestructura transversal (auditoría, autenticación), no lógica de negocio**. `Meals` y `Planner` siguen sin saber nada el uno del otro — ninguno referencia al otro directamente, solo ambos referencian `BuildingBlocks`. Es la diferencia entre "compartir un concepto de plomería" (aceptable) y "compartir reglas de negocio" (rompería el aislamiento de verdad).

### 16.2 Las interfaces de auditoría

```csharp
// BuildingBlocks.Domain/Auditing/IAuditableEntity.cs
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}

// BuildingBlocks.Domain/Auditing/ISoftDeletable.cs
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

Cada entidad (`Plato`, `Ingrediente`, `HistorialEntry`, `Usuario`) implementa ambas interfaces y declara las 7 propiedades. Sí, se repiten las mismas 7 líneas en cada entidad — con interfaces (en vez de una clase base abstracta) cada entidad sigue siendo una clase plana normal, sin forzar una jerarquía de herencia que no pedimos. `CreatedBy`/`UpdatedBy`/`DeletedBy` guardan el **Id del usuario** (no el username): es el identificador estable, no cambia si el usuario edita su nombre después.

### 16.3 `AuditableDbContext` — la "función automática" que pediste

Esta es la pieza que responde literalmente al pedido de "en el context debería definirse una función para que lo coloque automáticamente":

```csharp
public abstract class AuditableDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added) { entry.Entity.CreatedAt = now; entry.Entity.CreatedBy = userId; }
            else if (entry.State == EntityState.Modified) { entry.Entity.UpdatedAt = now; entry.Entity.UpdatedBy = userId; }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;   // convierte el DELETE en UPDATE
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
                entry.Entity.DeletedBy = userId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

**Cómo leerlo:**
- `ChangeTracker.Entries<T>()` es EF Core preguntándose a sí mismo "¿qué entidades rastreadas implementan esta interfaz, y en qué estado están (Added/Modified/Deleted)?" — es el mismo mecanismo interno que EF usa para decidir qué SQL generar, aprovechado acá para inspeccionar antes de guardar.
- El truco de soft-delete está en `entry.State = EntityState.Modified` — le miente a EF Core: "esto no es un DELETE, es un UPDATE". EF genera un `UPDATE ... SET "IsDeleted" = true` en vez de un `DELETE FROM ...`. **Ningún repositorio se enteró de este cambio** — `PlatoRepository.Remove(plato)` sigue llamando `_dbContext.Platos.Remove(plato)` exactamente igual que antes; la conversión pasa transparentemente acá, una sola vez, para todos los servicios.
- `MealsDbContext`, `PlannerDbContext` y `AuthDbContext` heredan de `AuditableDbContext` en vez de `DbContext` directo, y no necesitan sobreescribir `SaveChangesAsync` — lo heredan.

Query filter global (para que un registro "borrado" deje de aparecer en los `GET` normales), aplicado por reflexión sobre todas las entidades que implementan `ISoftDeletable`:

```csharp
protected static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
{
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;
        // arma dinámicamente: modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted)
    }
}
```
Cada `DbContext` concreto llama a `ApplySoftDeleteQueryFilters(modelBuilder)` como última línea de su `OnModelCreating` — es la única línea manual que hay que agregar por servicio; el resto es 100% automático.

### 16.4 `ICurrentUserService` — cómo el `DbContext` sabe quién sos

El `DbContext` no tiene acceso directo al usuario autenticado — eso vive en el `HttpContext` de la request HTTP, una capa arriba. El puente es `ICurrentUserService`, implementado leyendo los *claims* (los datos embebidos en el JWT) del usuario actual:

```csharp
public class CurrentUserService : ICurrentUserService
{
    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    // ...
}
```
Se registra como `AddScoped` (una instancia por request) e inyecta en el `DbContext` por constructor — el mismo patrón de inyección de dependencias que ya usan `PlatoService`/`PlatoRepository`.

### 16.5 El servicio `Auth`

Mismas 4 capas que Meals/Planner (`Auth.Domain/Application/Infrastructure/Api`), con la entidad `Usuario` (`Username`, `Email`, `PasswordHash`, `Rol`). Dos endpoints públicos (sin `RequireAuthorization()`):

- `POST /auth/register` — hashea la password con **BCrypt** (nunca se guarda en texto plano) y crea el usuario con rol `Usuario` por defecto.
- `POST /auth/login` — verifica la password contra el hash guardado y, si es válida, devuelve un JWT firmado.

El JWT incluye como claims: `sub`/`NameIdentifier` (Id del usuario), `Name` (username), `Role` (rol), y expira a los 60 minutos (`Jwt:ExpirationMinutes`). Se firma con `HMAC-SHA256` usando una clave simétrica.

### 16.6 Cómo se comparte la clave entre servicios

Los 4 servicios (`Auth`, `Meals`, `Planner`, y cualquier futuro) validan el **mismo token** con la **misma clave** — sin que `Meals.Api` tenga que llamar de vuelta a `Auth.Api` en cada request para preguntar "¿este token es válido?" (eso agregaría latencia y un punto único de fallo). Como todos comparten la clave simétrica, cada uno valida el JWT **localmente**, solo con matemática, en microsegundos.

La clave viaja por la variable de entorno `JWT_SIGNING_KEY` — mismo patrón que `POSTGRES_PASSWORD` (sección 9): nunca en el JSON commiteado, sí en `appsettings.Development.json` (gitignorado) para local y en el `.env` del VPS para producción. La config no-secreta (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes`) sí vive en `appsettings.json` porque no es sensible.

Para no repetir la configuración de `AddJwtBearer(...)` en 4 `Program.cs`, existe una extensión compartida:
```csharp
// BuildingBlocks.Infrastructure/Auth/JwtAuthenticationExtensions.cs
builder.Services.AddSharedJwtAuthentication(builder.Configuration);
```

Y cada grupo de endpoints que debe protegerse agrega una sola palabra:
```csharp
var group = app.MapGroup("/platos").WithTags("Platos").RequireAuthorization();
```
El grupo de `Auth` (`/register`, `/login`) es el único que **no** la lleva — tiene que ser público, es donde se consigue el token en primer lugar.

### 16.7 Gateway

YARP reenvía el header `Authorization` tal cual, sin tocarlo — no valida el JWT él mismo, cada servicio lo valida de forma independiente (si `Meals.Api` cae, `Planner.Api` sigue validando tokens sin depender de nada más). Se agregó la ruta `/auth/{**catch-all}` → `auth-cluster`, mismo patrón que `/meals` y `/planner` (sección 8).

### 16.8 Verificado end-to-end contra el VPS real

`POST /auth/register` → `POST /auth/login` (JWT) → `POST /platos` sin token (**401**) → `POST /platos` con token (**201**, y `CreatedBy`/`CreatedAt` quedaron poblados solos en la fila, verificado con `psql` directo) → `DELETE /platos/{id}` (**204**) → la fila **sigue existiendo** en la base con `IsDeleted=true`, `DeletedBy`/`DeletedAt` poblados, pero `GET /platos` ya no la lista. Se repitió el mismo flujo contra `Planner` (`/historial`) sin escribir una sola línea de código de auditoría ahí — todo vino de `BuildingBlocks`.

## 17. Logging estructurado con Serilog

Hasta acá, el único log era el default de ASP.NET Core: texto plano en consola, sin estructura, sin saber quién había hecho cada request. Esta sección agrega **Serilog** para registrar cada acción HTTP de forma estructurada, en consola y en archivo, en los 4 servicios — con el mismo criterio de "configurarlo una sola vez en `BuildingBlocks` y que todos lo hereden gratis" que ya se usó para JWT y auditoría (secciones 15 y 16).

### 17.1 Por qué Serilog y no el logger default

El `ILogger` que trae ASP.NET Core por defecto funciona, pero cada línea es texto suelto — no hay forma fácil de decir "dame todos los logs del usuario 3" o "dame todos los 500 de las últimas 24 horas" sin parsear texto a mano. **Serilog** trata cada entrada de log como un conjunto de **propiedades con nombre** (un objeto, no una frase), y recién al final decide cómo se ve esa entrada en cada destino (consola en texto legible, archivo en texto legible también, o JSON si hiciera falta después). Es el mismo cambio de mentalidad que "Contracts en vez de texto libre" (pregunta anterior sobre DTOs) pero aplicado a logs en vez de a las respuestas HTTP.

### 17.2 La extensión compartida

```csharp
// BuildingBlocks.Infrastructure/Logging/SerilogExtensions.cs
public static class SerilogExtensions
{
    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";

    public static void UseSharedSerilog(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(outputTemplate: OutputTemplate)
            .WriteTo.File(
                path: $"logs/{serviceName}-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: OutputTemplate));
    }

    public static void UseSharedRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("UserId", httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier));
            };
        });
    }
}
```

**Cómo leerlo:**
- `UseSharedSerilog(serviceName)` se llama **al principio** de `Program.cs`, antes de cualquier otro `builder.Services.AddX()` — reemplaza el logging provider por defecto de ASP.NET Core por Serilog para toda la app, incluyendo los logs internos de EF Core y del framework.
- `.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)` silencia el ruido interno de ASP.NET Core (que por defecto es bastante verboso en `Information`) y deja pasar solo warnings/errores de esa fuente, mientras el resto de la app sigue logueando en `Information`.
- `.Enrich.WithProperty("Service", serviceName)` agrega el nombre del servicio a **cada línea** — es lo que te permite, si más adelante centralizás logs de los 4 servicios en un solo lugar, filtrar "solo Auth.Api" o "solo Meals.Api".
- `UseSharedRequestLogging()` reemplaza el logging de requests que trae ASP.NET Core (varias líneas por request, dispersas) por **una sola línea estructurada por request** vía `UseSerilogRequestLogging` de `Serilog.AspNetCore` — método, ruta, código de estado, y duración en milisegundos.
- `EnrichDiagnosticContext` es el gancho que agrega el `UserId` a esa línea de request: lee el claim `NameIdentifier` del usuario autenticado (el mismo mecanismo que usa `CurrentUserService`, sección 16.4) y lo adjunta como propiedad — `null` si la request no llevaba JWT válido.

### 17.3 Por qué se ve `{Properties:j}` en cada línea

El `outputTemplate` incluye `{Properties:j}` — todas las propiedades adjuntas a la entrada de log (que no aparecen ya en el mensaje principal) se imprimen como JSON compacto al final de la línea. Ejemplo real, capturado en pruebas:

```
2026-08-18 02:58:32.635 -05:00 [INF] HTTP GET /platos responded 200 in 2100.0393 ms {"UserId":"2","SourceContext":"Serilog.AspNetCore.RequestLoggingMiddleware","RequestId":"...","Service":"Meals.Api"}
2026-08-18 02:58:32.681 -05:00 [INF] HTTP GET /platos responded 401 in 4.7667 ms {"UserId":null,"SourceContext":"...","Service":"Meals.Api"}
```

Sin `{Properties:j}` en el template, el `UserId` seguiría existiendo internamente (Serilog lo guarda igual), pero **no se vería** en el texto de la línea — quedaría invisible salvo que se use un sink que sepa leer propiedades estructuradas (como Seq). Como se decidió no sumar esa infraestructura por ahora, mostrar las propiedades en texto plano es lo que hace que "saber quién hizo la acción" sea usable con solo abrir el archivo.

### 17.4 Dónde quedan los logs

Cada servicio escribe a una ruta relativa `logs/{servicio}-.log` (relativa al directorio de trabajo del proceso — dentro del contenedor es `/app`, por el `WORKDIR /app` del Dockerfile). `RollingInterval.Day` genera un archivo nuevo por día (`Meals.Api-20260818.log`, `Meals.Api-20260819.log`, ...) y `retainedFileCountLimit: 14` borra automáticamente los de más de 14 días.

En `docker-compose.yml`, cada servicio monta esa carpeta a un path del VPS:
```yaml
meals-api:
  volumes:
    - ./logs/meals-api:/app/logs
```
Así los logs quedan en `~/comidiaria/backend-net/logs/meals-api/` en el VPS, visibles con `tail -f` o `cat` directo, sin depender de `docker logs` (que se pierde si el contenedor se recrea). **Pendiente de verificar en el próximo deploy** (no se hizo en este alcance): las imágenes `aspnet` recientes corren como usuario no-root dentro del contenedor por defecto — si Docker crea la carpeta del bind mount como `root` en el host la primera vez, el proceso podría no tener permiso de escritura. Si pasa, se soluciona con `mkdir -p logs/{servicio}` a mano en el VPS antes del primer `docker compose up`, o ajustando el owner con `chown`.

### 17.5 Qué se probó

Se corrieron `Meals.Api`, `Planner.Api` y `Auth.Api` localmente contra la base real del VPS (vía túnel SSH) y se confirmó: la consola ya no muestra el formato default de ASP.NET Core sino el de Serilog; cada request genera una línea; el archivo `logs/{Servicio}-<fecha>.log` se crea con el mismo contenido; una request con JWT válido muestra `"UserId":"<id>"` y una sin token muestra `"UserId":null`; y el campo `"Service"` distingue correctamente el origen al probar `Meals.Api` y `Planner.Api` en paralelo.

## 18. Bug real en el primer deploy con `BuildingBlocks`: `Directory.Packages.props` faltante en los Dockerfiles

Al desplegar la versión con `Auth`, JWT y Serilog (`v0.2.0`), el pipeline de GitHub Actions reportó **éxito**, pero el VPS no actualizó ningún contenedor y `auth-api` ni siquiera existía. Dos bugs distintos se combinaron para que el fallo pasara desapercibido.

### 18.1 El bug real: ningún Dockerfile copiaba `Directory.Packages.props`

Desde que se introdujo Central Package Management (sección 13.3), todos los `.csproj` de la solución dejaron de declarar `Version="..."` en sus `PackageReference` — la versión se resuelve leyendo `Directory.Packages.props` en la raíz del repo. Eso funciona perfecto con `dotnet build` desde la raíz (MSBuild encuentra el archivo subiendo por el árbol de carpetas), pero **ningún Dockerfile fue actualizado para copiar ese archivo** al contexto de build antes de correr `dotnet restore`. Dentro del contenedor, el `.csproj` quedaba aislado sin acceso a `Directory.Packages.props`, y el build fallaba con:
```
error NU1015: The following PackageReference item(s) do not have a version specified: ...
```
Esto afectaba a **los 4 servicios**, no solo al nuevo `Auth` — simplemente nunca se había notado porque, desde que se agregó CPM, no se había vuelto a correr un build Docker real hasta este deploy (los intentos anteriores de `docker compose build` fallaron por Docker Desktop apagado localmente, sección 13, y quedaron sin verificar).

**Fix**: agregar una línea a cada Dockerfile, justo después del primer `WORKDIR /src`, antes de cualquier otro `COPY`:
```dockerfile
COPY ["Directory.Packages.props", "./"]
```

El `ApiGateway` tenía además un segundo problema: nunca se actualizó su Dockerfile para copiar los `.csproj` de `BuildingBlocks/` cuando se le agregó esa referencia (sección 17), así que ni siquiera encontraba el proyecto.

### 18.2 Por qué el pipeline no avisó del error

El script del workflow (`.github/workflows/deploy.yml`) corre varios comandos en secuencia dentro de un solo bloque `script:` de `appleboy/ssh-action`, **sin `set -e`**:
```yaml
script: |
  cd ~/comidiaria/backend-net
  git fetch --tags --force
  git checkout ${{ github.ref_name }}
  docker compose build
  docker compose up -d
  docker image prune -f
```
Sin `set -e`, un bash script sigue ejecutando la siguiente línea aunque la anterior falle (a diferencia de encadenar con `&&`). `docker compose build` falló, pero el script igual llegó a `docker compose up -d` (que no tenía nada nuevo que levantar, así que "tuvo éxito" trivialmente) y a `docker image prune -f` (que también tuvo éxito). El **último** comando del script determina el código de salida que ve `appleboy/ssh-action` — como fue exitoso, GitHub Actions marcó todo el job en verde, ocultando que el paso realmente importante había fallado en silencio.

**Fix**: agregar `set -e` como primera línea del script — ahora cualquier comando que falle corta la ejecución inmediatamente y el job queda en rojo, reflejando la realidad.

### 18.3 Cómo se detectó y verificó

Después del "deploy exitoso", se notó que los contenedores en el VPS seguían con el mismo *uptime* de antes (no se habían recreado) y que `auth-api` no aparecía en `docker ps`. Se corrió `docker compose build` manualmente por SSH para reproducir el error real, se aplicó el fix en los 4 Dockerfiles + el workflow, se copiaron los Dockerfiles corregidos al VPS para una prueba manual (`docker compose build` completo, exitoso) y se levantaron los contenedores (`docker compose up -d`) — confirmando por primera vez `auth-api` corriendo. Se probó el flujo público a través del Gateway (`/auth/register` → 201, `/meals/platos` y `/planner/historial` sin token → 401) y se confirmó el log de Serilog escribiendo en `~/comidiaria/backend-net/logs/meals-api/`. El fix se subió a `main` y quedará validado en el próximo tag, que además ahora fallará ruidosamente si algo se rompe.
