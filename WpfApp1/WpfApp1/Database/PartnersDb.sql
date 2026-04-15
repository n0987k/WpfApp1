-- Справочная схема для приложения WpfApp1.
-- Скрипт безопасно повторять: таблицы создаются только если их ещё нет.
-- Если база из .bacpac, обычно уже есть Users и др. — блоки IF OBJECT_ID просто пропускаются.

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Login NVARCHAR(100) NOT NULL,
        Password NVARCHAR(200) NOT NULL,
        Role NVARCHAR(50) NULL,
        FullName NVARCHAR(200) NULL
    );
END;

IF OBJECT_ID(N'dbo.Partners', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Partners
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        DirectorName NVARCHAR(200) NULL,
        Phone NVARCHAR(50) NULL,
        Email NVARCHAR(200) NULL,
        Address NVARCHAR(500) NULL
    );
END;

IF OBJECT_ID(N'dbo.PartnerSales', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartnerSales
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PartnerId INT NOT NULL,
        ProductName NVARCHAR(200) NULL,
        Quantity INT NOT NULL,
        SaleDate DATETIME NOT NULL,
        CONSTRAINT FK_PartnerSales_Partners FOREIGN KEY (PartnerId) REFERENCES dbo.Partners(Id)
    );
END;

-- Типы материалов и склад (вкладка «Склад и материалы» ищет таблицу Material).
IF OBJECT_ID(N'dbo.MaterialType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MaterialType
    (
        MaterialTypeID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MaterialTypeName NVARCHAR(200) NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.Material', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Material
    (
        MaterialID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MaterialTypeID INT NOT NULL,
        MaterialName NVARCHAR(200) NOT NULL,
        QuantityOnStock DECIMAL(18, 2) NOT NULL DEFAULT (0),
        Unit NVARCHAR(20) NULL,
        CONSTRAINT FK_Material_MaterialType FOREIGN KEY (MaterialTypeID) REFERENCES dbo.MaterialType(MaterialTypeID)
    );
END;

-- Производство (вкладка ищет Workshop, Production или Manufacture).
IF OBJECT_ID(N'dbo.Workshop', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Workshop
    (
        WorkshopID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        WorkshopName NVARCHAR(200) NOT NULL,
        Address NVARCHAR(500) NULL
    );
END;

-- Сотрудники (при отсутствии своей таблицы в .bacpac).
IF OBJECT_ID(N'dbo.Employee', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employee
    (
        EmployeeID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FullName NVARCHAR(200) NOT NULL,
        Position NVARCHAR(100) NULL,
        Phone NVARCHAR(50) NULL
    );
END;

-- Пример пользователя:
-- INSERT INTO dbo.Users(Login, Password, Role, FullName) VALUES (N'admin', N'12345', N'Admin', N'Администратор');
--
-- Если база уже развёрнута из .bacpac без столбца FullName:
-- ALTER TABLE dbo.Users ADD FullName NVARCHAR(200) NULL;
--
-- Примеры данных для склада и производства:
-- INSERT INTO dbo.MaterialType(MaterialTypeName) VALUES (N'Лаки'), (N'Доски');
-- INSERT INTO dbo.Material(MaterialTypeID, MaterialName, QuantityOnStock, Unit) VALUES (1, N'Лак матовый 10 л', 120, N'шт');
-- INSERT INTO dbo.Workshop(WorkshopName, Address) VALUES (N'Цех №1', N'г. Москва, ул. Заводская, 1');
