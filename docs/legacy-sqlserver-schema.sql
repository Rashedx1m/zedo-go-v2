-- جدول المستخدمين
CREATE TABLE [Users] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [FullName] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(100) NOT NULL,
    [Phone] NVARCHAR(20) NOT NULL,
    [PasswordHash] NVARCHAR(200) NOT NULL,
    [Role] INT NOT NULL DEFAULT 1,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [LastLoginAt] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL
);
CREATE UNIQUE INDEX IX_Users_Email ON [Users]([Email]);
CREATE UNIQUE INDEX IX_Users_Phone ON [Users]([Phone]);

-- جدول العملاء
CREATE TABLE [Customers] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [UserId] INT NOT NULL,
    [Rating] DECIMAL(3,2) NOT NULL DEFAULT 5.0,
    [TotalRides] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    CONSTRAINT FK_Customers_Users FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
);

-- جدول السائقين
CREATE TABLE [Drivers] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [UserId] INT NOT NULL,
    [CarModel] NVARCHAR(50) NOT NULL,
    [CarColor] NVARCHAR(30) NOT NULL,
    [PlateNumber] NVARCHAR(20) NOT NULL,
    [LicenseNumber] NVARCHAR(50) NOT NULL,
    [Status] INT NOT NULL DEFAULT 0,
    [Rating] DECIMAL(3,2) NOT NULL DEFAULT 5.0,
    [TotalRides] INT NOT NULL DEFAULT 0,
    [TotalEarnings] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    CONSTRAINT FK_Drivers_Users FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_Drivers_PlateNumber ON [Drivers]([PlateNumber]);

-- جدول مواقع السائقين
CREATE TABLE [DriverLocations] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [DriverId] INT NOT NULL,
    [Latitude] FLOAT NOT NULL,
    [Longitude] FLOAT NOT NULL,
    [LastUpdate] DATETIME2 NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    CONSTRAINT FK_DriverLocations_Drivers FOREIGN KEY ([DriverId]) REFERENCES [Drivers]([Id]) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_DriverLocations_DriverId ON [DriverLocations]([DriverId]);

-- جدول الطلبات
CREATE TABLE [Requests] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [CustomerId] INT NOT NULL,
    [DriverId] INT NULL,
    [PickupLatitude] FLOAT NOT NULL,
    [PickupLongitude] FLOAT NOT NULL,
    [DropLatitude] FLOAT NOT NULL,
    [DropLongitude] FLOAT NOT NULL,
    [Status] INT NOT NULL DEFAULT 1,
    [EstimatedCost] DECIMAL(18,2) NULL,
    [ActualCost] DECIMAL(18,2) NULL,
    [DistanceKm] FLOAT NULL,
    [DurationMinutes] INT NULL,
    [AcceptedAt] DATETIME2 NULL,
    [ArrivedAt] DATETIME2 NULL,
    [StartedAt] DATETIME2 NULL,
    [CompletedAt] DATETIME2 NULL,
    [CanceledAt] DATETIME2 NULL,
    [CancellationReason] NVARCHAR(500) NULL,
    [CanceledBy] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    CONSTRAINT FK_Requests_Customers FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]),
    CONSTRAINT FK_Requests_Drivers FOREIGN KEY ([DriverId]) REFERENCES [Drivers]([Id])
);
CREATE INDEX IX_Requests_Status ON [Requests]([Status]);
CREATE INDEX IX_Requests_CreatedAt ON [Requests]([CreatedAt]);

-- جدول المدفوعات
CREATE TABLE [Payments] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [RequestId] INT NOT NULL,
    [CustomerId] INT NOT NULL,
    [DriverId] INT NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [CompanyCommission] DECIMAL(18,2) NOT NULL,
    [DriverEarning] DECIMAL(18,2) NOT NULL,
    [Method] INT NOT NULL,
    [Status] INT NOT NULL DEFAULT 1,
    [TransactionId] NVARCHAR(100) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    CONSTRAINT FK_Payments_Requests FOREIGN KEY ([RequestId]) REFERENCES [Requests]([Id])
);
CREATE UNIQUE INDEX IX_Payments_RequestId ON [Payments]([RequestId]);

-- جدول التسعير
CREATE TABLE [RidePricings] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL,
    [BaseFare] DECIMAL(18,2) NOT NULL,
    [PricePerKm] DECIMAL(18,2) NOT NULL,
    [PricePerMinute] DECIMAL(18,2) NOT NULL,
    [MinimumFare] DECIMAL(18,2) NOT NULL DEFAULT 10.0,
    [IsActive] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL
);

-- إضافة تسعيرة افتراضية
INSERT INTO [RidePricings] ([Name], [BaseFare], [PricePerKm], [PricePerMinute], [MinimumFare], [IsActive])
VALUES (N'التسعيرة الافتراضية', 5.0, 2.5, 0.5, 10.0, 1);