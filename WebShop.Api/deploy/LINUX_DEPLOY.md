# Linux Deployment (WebShop API)

## 1. Publish on build machine

```bash
dotnet publish WebShop.Api/WebShop.Api.csproj -c Release -o ./publish/api
```

## 2. Copy publish output to server

Copy all files from `publish/api` to:

```bash
/opt/webshop-api
```

## 3. Install .NET 8 ASP.NET Runtime on server

Install using Microsoft package feed for your distro.

## 4. Run once manually (sanity check)

```bash
cd /opt/webshop-api
dotnet WebShop.Api.dll
```

The API defaults to `http://0.0.0.0:5088` unless `ASPNETCORE_URLS` is set.

## 5. Enable systemd service

Copy service file:

```bash
sudo cp deploy/systemd/webshop-api.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable webshop-api
sudo systemctl start webshop-api
sudo systemctl status webshop-api
```

## 6. Configure Nginx reverse proxy

Copy config:

```bash
sudo cp deploy/nginx/webshop-api.conf /etc/nginx/sites-available/webshop-api.conf
sudo ln -s /etc/nginx/sites-available/webshop-api.conf /etc/nginx/sites-enabled/webshop-api.conf
sudo nginx -t
sudo systemctl reload nginx
```

## 7. HTTPS (recommended)

Use certbot or your preferred TLS method on `api.example.com`.

## Notes

- DB files and initialization scripts are published automatically from `Database/`.
- Logs are written to `Logs/requests.log` under the API working directory.
- Ensure service user has write permissions to `Database/` and `Logs/`.
