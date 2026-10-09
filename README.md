# Biblioteca de Prácticas

Plataforma web para centralizar, organizar y consultar el material académico en PDF de estudiantes de medicina. Proyecto de servicio social de la Facultad de Ciencias de la Computación, BUAP, para la Facultad de Medicina.

## Qué hace

**Para estudiantes y docentes**
- Catálogo con buscador por título o autor, y filtros por **categoría** y por **semestre**.
- **Lectura parcial sin cuenta** (cerca del 25 % del libro, mínimo 5 páginas). El recorte se hace en el servidor: las páginas restantes no salen sin sesión.
- Con cuenta: lectura completa, descarga, favoritos y contador de descargas.
- Registro solo con correo institucional de la BUAP (`@alumno.buap.mx`, `@correo.buap.mx` o `@buap.mx`), con **verificación por código de 6 dígitos** enviado al correo.
- Recuperación de contraseña por código.

**Para administradores**
- Subida de varios PDF a la vez, con portada, tabla de contenido y número de páginas generados automáticamente.
- Gestión de libros, categorías (sin duplicados) y usuarios (rol, alta y baja).

## Tecnologías

| Capa | Tecnología |
|---|---|
| Interfaz | Angular, Tailwind CSS |
| API | ASP.NET Core (.NET 10), Entity Framework Core, JWT |
| Base de datos | PostgreSQL (Docker) |
| Pruebas | xUnit (backend) |

## Estructura

```
BibliotecaDigital/
├── BibliotecaPracticas.api/          API (.NET)
├── BibliotecaPracticas.api.Tests/    Pruebas unitarias del backend
├── biblioteca-practicas.client/      Interfaz (Angular)
└── docker-compose.yml                Base de datos
```

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org) LTS y Angular CLI (`npm i -g @angular/cli`)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Herramienta de migraciones: `dotnet tool install --global dotnet-ef`

## Puesta en marcha

### 1. Base de datos

Crea un archivo `.env` en la raíz (no se sube a git):

```
POSTGRES_DB=BibliotecaPracticasDB
POSTGRES_USER=tu_usuario
POSTGRES_PASSWORD=tu_password
```

Levanta la base de datos:

```
docker compose up -d
```

Si el puerto 5432 está ocupado por otra base de datos, cambia el mapeo en `docker-compose.yml` (por ejemplo `"5433:5432"`) y usa ese puerto en la cadena de conexión.

### 2. Configuración de la API

`BibliotecaPracticas.api/appsettings.json` se sube con valores de relleno (`CAMBIAR`). Crea `BibliotecaPracticas.api/appsettings.Development.json` (ignorado por git) con tus valores reales:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=BibliotecaPracticasDB;Username=TU_USUARIO;Password=TU_PASSWORD"
  },
  "Jwt": {
    "Key": "clave-aleatoria-de-32-caracteres-o-mas",
    "Issuer": "BibliotecaPracticasApi",
    "Audience": "BibliotecaPracticasClient",
    "ExpiresInMinutes": "60"
  },
  "Correo": {
    "ModoConsola": true
  }
}
```


### 3. Migraciones y API

```
cd BibliotecaPracticas.api
dotnet ef database update
dotnet run
```

La API queda en `http://localhost:5250` (Swagger en `/swagger`).

### 4. Interfaz

```
cd biblioteca-practicas.client
npm install
ng serve
```

Abre `http://localhost:4200`.

### 5. Primer administrador

Regístrate desde la interfaz y cambia tu rol a `Admin` (valor `0`) directamente en la tabla `Usuarios`. Después podrás gestionar los demás roles desde **Administración → Usuarios**.

## Correo (verificación y recuperación de contraseña)

Con `"ModoConsola": true` los códigos **no se envían**: aparecen en la terminal de la API (`[MODO CONSOLA] ...`). Sirve para desarrollo.

Para enviarlos de verdad, configura la sección `Correo` con un servidor SMTP (Gmail con contraseña de aplicación, Brevo o el servidor de la BUAP):

| Campo | Descripción |
|---|---|
| `ModoConsola` | `false` en producción |
| `Servidor`, `Puerto` | Servidor SMTP y puerto (normalmente 587) |
| `Usuario`, `Clave` | Credenciales SMTP. En Gmail, una contraseña de aplicación |
| `Remitente` | Correo visible como remitente; vacío usa `Usuario` |
| `NombreRemitente` | Nombre que se muestra |

En producción define los secretos con variables de entorno (por ejemplo `Correo__Clave`), nunca en el repositorio. Más detalle en el Anexo A del manual de usuario.

## Pruebas

```
dotnet test BibliotecaPracticas.api.Tests
```

Cubren registro, verificación de correo, login, recuperación de contraseña, categorías, favoritos, protección de archivos, lectura parcial y roles de seguridad.

## Antes de publicar

- [ ] `Correo:ModoConsola` en `false` con SMTP configurado.
- [ ] `Jwt:Key` nueva, larga y aleatoria; contraseña de base de datos propia.
- [ ] Servir con **HTTPS**.
- [ ] Cambiar `http://localhost:5250` (servicios de Angular) y `http://localhost:4200` (CORS en `Program.cs`) por las direcciones reales.

## Limitaciones conocidas

- La sesión se guarda en `localStorage`; como mejora se plantea migrar a cookies `HttpOnly`.
- Un cambio de contraseña no cierra las sesiones ya abiertas hasta que su token expire.
- Los libros subidos viven en `wwwroot/uploads/` y no se incluyen en el repositorio.

## Documentación

Manual de usuario: **[enlace o ruta al PDF]**

## Autor

Pérez Méndez José Mario, servicio social, BUAP, 2026.
Responsable: Dr. Ariel Farit Gutiérrez Alexander, Facultad de Medicina.