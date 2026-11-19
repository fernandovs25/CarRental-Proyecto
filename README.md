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
