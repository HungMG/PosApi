# 1. Kéo môi trường chạy (Runtime) của .NET 10
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# 2. Kéo công cụ Build (SDK) của .NET 10 để dịch code
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy file cấu hình .csproj vào trước để khôi phục thư viện (Restore)
COPY ["PosWebAdmin.csproj", "./"]
RUN dotnet restore "./PosWebAdmin.csproj"

# Copy toàn bộ code còn lại vào và tiến hành Build
COPY . .
WORKDIR "/src/."
RUN dotnet build "PosWebAdmin.csproj" -c Release -o /app/build

# 3. Đóng gói (Publish) ứng dụng cho nhẹ
FROM build AS publish
RUN dotnet publish "PosWebAdmin.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Gắn code đã đóng gói vào môi trường chạy và khởi động Web
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
