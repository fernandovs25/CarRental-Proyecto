IF DB_ID(N'CarRentalAssignment') IS NULL
    CREATE DATABASE CarRentalAssignment;
GO
USE CarRentalAssignment;
GO
IF OBJECT_ID(N'dbo.isoCountries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.isoCountries
    (
        CountryId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryName varchar(100) NOT NULL,
        Iso2 char(2) NOT NULL UNIQUE,
        Iso3 char(3) NOT NULL UNIQUE
    );
END;
GO
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        IdCustomer int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FirstName varchar(50) NOT NULL,
        LastName varchar(50) NOT NULL,
        SSN varchar(30) NOT NULL,
        Email varchar(254) NOT NULL UNIQUE,
        CountryId int NOT NULL REFERENCES dbo.isoCountries(CountryId),
        Mobile varchar(30) NOT NULL,
        StateName varchar(100) NOT NULL
    );
END;
GO
CREATE OR ALTER PROCEDURE dbo.GetCountries
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CountryId, CountryName, Iso2, Iso3 FROM dbo.isoCountries ORDER BY CountryId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.GetCountryById @CountryId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CountryId, CountryName, Iso2, Iso3 FROM dbo.isoCountries WHERE CountryId = @CountryId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.InsertCountry
    @CountryName varchar(100), @Iso2 char(2), @Iso3 char(3)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.isoCountries (CountryName, Iso2, Iso3) VALUES (@CountryName, @Iso2, @Iso3);
    SELECT CAST(SCOPE_IDENTITY() AS int) AS newCountryId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.UpdateCountry
    @CountryName varchar(100), @Iso2 char(2), @Iso3 char(3), @CountryId int
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.isoCountries SET CountryName = @CountryName, Iso2 = @Iso2, Iso3 = @Iso3 WHERE CountryId = @CountryId;
    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO
CREATE OR ALTER PROCEDURE dbo.GetCustomers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT customer.IdCustomer, customer.FirstName, customer.LastName, customer.SSN,
           customer.Email, customer.CountryId, country.CountryName AS Country,
           customer.Mobile, customer.StateName
    FROM dbo.Customers AS customer
    INNER JOIN dbo.isoCountries AS country ON country.CountryId = customer.CountryId
    ORDER BY customer.IdCustomer;
END;
GO
CREATE OR ALTER PROCEDURE dbo.GetCustomerById @customerId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT customer.IdCustomer, customer.FirstName, customer.LastName, customer.SSN,
           customer.Email, customer.CountryId, country.CountryName AS Country,
           customer.Mobile, customer.StateName
    FROM dbo.Customers AS customer
    INNER JOIN dbo.isoCountries AS country ON country.CountryId = customer.CountryId
    WHERE customer.IdCustomer = @customerId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.InsertCustomer
    @FirstName varchar(50), @LastName varchar(50), @SSN varchar(30), @Email varchar(254),
    @CountryId int, @Mobile varchar(30), @StateName varchar(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Customers (FirstName, LastName, SSN, Email, CountryId, Mobile, StateName)
    VALUES (@FirstName, @LastName, @SSN, @Email, @CountryId, @Mobile, @StateName);
    SELECT CAST(SCOPE_IDENTITY() AS int) AS newCustomerId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.UpdateCustomer
    @FirstName varchar(50), @LastName varchar(50), @SSN varchar(30), @Email varchar(254),
    @CountryId int, @Mobile varchar(30), @StateName varchar(100), @customerId int
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Customers SET FirstName = @FirstName, LastName = @LastName, SSN = @SSN,
        Email = @Email, CountryId = @CountryId, Mobile = @Mobile, StateName = @StateName
    WHERE IdCustomer = @customerId;
    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO
INSERT INTO dbo.isoCountries (CountryName, Iso2, Iso3)
SELECT sample.CountryName, sample.Iso2, sample.Iso3
FROM (VALUES ('Mexico', 'MX', 'MEX'), ('Canada', 'CA', 'CAN'), ('United States', 'US', 'USA'))
    AS sample(CountryName, Iso2, Iso3)
WHERE NOT EXISTS (SELECT 1 FROM dbo.isoCountries WHERE Iso2 = sample.Iso2);
GO
INSERT INTO dbo.Customers (FirstName, LastName, SSN, Email, CountryId, Mobile, StateName)
SELECT sample.FirstName, sample.LastName, sample.SSN, sample.Email, country.CountryId, sample.Mobile, sample.StateName
FROM (VALUES
    ('Ana', 'Lopez', 'TEST-001', 'ana@example.test', 'MX', '5550101', 'Quintana Roo'),
    ('Luis', 'Garcia', 'TEST-002', 'luis@example.test', 'CA', '5550102', 'Ontario'),
    ('Maria', 'Torres', 'TEST-003', 'maria@example.test', 'US', '5550103', 'Florida')
) AS sample(FirstName, LastName, SSN, Email, Iso2, Mobile, StateName)
INNER JOIN dbo.isoCountries AS country ON country.Iso2 = sample.Iso2
WHERE NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = sample.Email);
GO