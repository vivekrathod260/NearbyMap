# 🗺️ NearbyMap

A **.NET 8 microservices-based backend** for discovering nearby businesses using **geohash-powered geospatial search**, **Redis caching**, and **gRPC communication**.

## 🚀 Features

- 🏢 Business CRUD operations
- 📍 Nearby search by location, radius, and category
- 🔢 Geohash-based location indexing
- ⚡ Redis caching for faster repeated queries
- 🔗 gRPC service-to-service communication
- ❤️ Health monitoring endpoints

## 🏗️ Architecture

### 🌐 API Gateway
Exposes REST APIs and routes requests to backend services.

### 🏢 Business Service
Manages business data and generates geohashes from coordinates.

### 📍 Proximity Service
Performs nearby searches using geohash lookups, caching, and distance filtering.

## ⚙️ Tech Stack

- .NET 8
- ASP.NET Core
- Entity Framework Core
- SQL Server
- Redis
- gRPC
- Geohash

## 💡 Highlights

- 📍 Optimized geospatial search using geohash indexing
- 🏗️ Scalable microservices architecture
- ⚡ Improved performance through Redis caching
- 🔗 Efficient inter-service communication with gRPC


## 📡 API Endpoints
```http
GET /api/nearby
```

Query Parameters:

- `lat`
- `lon`
- `radius`
- `category`
- `limit`

### 🏢 Business Management

```http
GET    /api/business/{id}
POST   /api/business
PUT    /api/business/{id}
DELETE /api/business/{id}
```

### ❤️ Health Check

```http
GET /health
```
