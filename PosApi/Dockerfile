FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Sao chép toàn bộ mã nguồn vào vùng build
COPY . .

# Khôi phục các gói NuGet và biên dịch dự án dưới dạng Release
RUN dotnet restore
RUN dotnet publish -c Release -o /app --no-restore

# Tạo môi trường chạy tinh gọn
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./

# Cấu hình để API lắng nghe cổng 10000 (Cổng mặc định của Render)
ENV ASPNETCORE_URLS=http://+:10000

ENTRYPOINT ["dotnet", "PosApi.dll"]