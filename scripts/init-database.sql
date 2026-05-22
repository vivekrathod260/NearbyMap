-- Proximity Service Database Schema
-- Optimized for read-heavy geospatial queries at 50k-100k QPS

CREATE DATABASE ProximityDb;
GO
USE ProximityDb;
GO

CREATE TABLE BusinessLocations (
    BusinessId NVARCHAR(36) NOT NULL PRIMARY KEY NONCLUSTERED,
    Name NVARCHAR(256) NOT NULL,
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,
    Geohash NVARCHAR(12) NOT NULL,
    Category NVARCHAR(64) NULL,
    Rating FLOAT NOT NULL DEFAULT 0,

    -- Clustered index on Geohash for range scan performance
    INDEX IX_BusinessLocation_Geohash CLUSTERED (Geohash),
    INDEX IX_BusinessLocation_Geohash_Category NONCLUSTERED (Geohash, Category) INCLUDE (BusinessId, Name, Latitude, Longitude, Rating)
);
GO

-- Business Service Database Schema
CREATE DATABASE BusinessDb;
GO
USE BusinessDb;
GO

CREATE TABLE Businesses (
    BusinessId NVARCHAR(36) NOT NULL PRIMARY KEY,
    Name NVARCHAR(256) NOT NULL,
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,
    Geohash NVARCHAR(12) NOT NULL,
    Category NVARCHAR(64) NULL,
    Address NVARCHAR(512) NULL,
    Phone NVARCHAR(32) NULL,
    Rating FLOAT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    INDEX IX_Business_Geohash NONCLUSTERED (Geohash)
);
GO

-- Partitioning strategy for 200M POIs
-- Partition by geohash prefix (first 2 chars = ~1024 partitions)
-- This distributes data geographically and enables partition elimination
