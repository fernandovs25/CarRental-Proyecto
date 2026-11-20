# CarRental API: paises y clientes

La API contiene los endpoints `GET`, `GET /{id}`, `POST` y `PUT` en `/IsoCountries` y `/Customers`. Los repositorios de estas entidades utilizan procedimientos almacenados de SQL Server.

## Requisitos

- SDK de .NET 10 y runtime de ASP.NET Core 8.
- SQL Server o SQL Server LocalDB y `sqlcmd`.

## Crear la base de desarrollo

Desde la raiz del repositorio, inicia LocalDB y ejecuta:

```powershell
SqlLocalDB.exe start MSSQLLocalDB
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -b -i Database/setup.sql
```

El script crea `CarRentalAssignment` si no existe, crea las tablas si faltan, crea o actualiza los ocho procedimientos y agrega tres paises y tres clientes ficticios. El script esta pensado para una base de desarrollo dedicada; no lo ejecutes sobre una base existente con un esquema diferente. No elimina tablas ni registros.

En desarrollo la API usa LocalDB mediante `CarRental/appsettings.Development.json`. Para otra instancia establece `ConnectionStrings__CarRental` como variable de entorno o mediante secretos de usuario; no incluyas contrasenas en el repositorio. La base de ejemplo cubre solo paises y clientes; los demas recursos existentes requieren sus propias tablas y configuracion.

## Ejecutar y probar

```powershell
dotnet test --project CarRental.Tests/CarRental.Tests.csproj
dotnet run --project CarRental/CarRental.csproj --urls http://localhost:5068
```

Abre `http://localhost:5068/swagger` y prueba ambos controladores. `POST` devuelve el identificador generado; `PUT` devuelve 204 si actualiza un registro o 404 si no existe. No se debe interpretar un error de conexion SQL como una lista vacia.

## Registro y acceso con token

La autenticacion usa MySQL/MariaDB y EF Core. Configura la contrasena de la base como secreto de usuario; no la escribas en archivos versionados:

```powershell
dotnet user-secrets init --project CarRental/CarRental.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1;Port=3306;Database=CarRentalDb;User=root;Password=TU_CLAVE" --project CarRental/CarRental.csproj
```

En PowerShell tambien puedes definir temporalmente `ConnectionStrings__DefaultConnection` antes de iniciar la API. Para MariaDB cambia `Database:Engine` a `MariaDb` y `Database:Version` por la version instalada. En desarrollo se genera una clave JWT temporal si no existe `Jwt:Key`; para despliegue configura una clave aleatoria de al menos 32 caracteres con User Secrets o un gestor de secretos:

```powershell
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))) --project CarRental/CarRental.csproj
```

Si la base todavía no existe, créala desde MySQL Workbench o el cliente MySQL con `CREATE DATABASE CarRentalDb;`. Instala la herramienta de migraciones y crea la tabla `Usuarios`:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.13
dotnet ef migrations add CrearUsuarios --project CarRental/CarRental.csproj
dotnet ef database update --project CarRental/CarRental.csproj
```

`POST /api/auth/register` recibe `username`, `email` y `password` (minimo 12 caracteres). El registro asigna el rol `User`; no acepta roles del cliente y guarda la contrasena con hash. `POST /api/auth/login` devuelve un JWT. En Swagger, pulsa `Authorize` y pega el token para probar `GET /api/auth/users/{id}` y `POST /api/vehiculos`; el perfil solo se muestra al usuario dueño o a un administrador. `DELETE /api/vehiculos/{id}` requiere rol `Admin`. Para promover una cuenta, un administrador de la base debe cambiar su rol manualmente despues de verificar su identidad, por ejemplo `UPDATE Usuarios SET Role = 'Admin' WHERE Username = 'admin_car';`; nunca se acepta el rol del registro publico. `GET /api/vehiculos` es publico.

Ejemplo para `POST /api/auth/register`:

```json
{ "username": "admin_car", "email": "admin@example.test", "password": "Password123!" }
```

Ejemplo para `POST /api/auth/login`:

```json
{ "username": "admin_car", "password": "Password123!" }
```

La respuesta contiene `token` y `expiresAt`. En Swagger usa `Authorize` con el token, sin escribir `Bearer` al inicio. Para probar `POST /api/vehiculos`, envia el texto JSON `"Toyota Prius"`.

Ejemplo de pais para `POST /IsoCountries`:

```json
{ "countryName": "Japan", "iso2": "JP", "iso3": "JPN" }
```

Ejemplo de cliente para `POST /Customers` (usa un `countryId` existente):

```json
{ "firstName": "Sofia", "lastName": "Diaz", "socialSecurityNumber": "TEST-004", "email": "sofia@example.test", "countryId": 1, "mobilePhone": "5550104", "stateName": "Yucatan" }
```

Para verificar los datos:

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d CarRentalAssignment -Q 'SELECT * FROM dbo.isoCountries; SELECT * FROM dbo.Customers;'
```
