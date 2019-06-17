# Proxy para realizar forwarding de requests .NET
### Requisitos para levantar el proyecto en desarrollo:
* Tener instalado visual studio y .NET Framework 4.5
### Procedimiento de apertura de proyecto y debug
* Clonar el repositorio
* Abrir el archivo NET Proxy.sln
* Descargar las dependencias de nuget
### Para cambiar el endpoint de destino (dentro del archivo `web.config`)
* `SG_ENDPOINT`: url remota (sin http/https)
* `SG_HOST`: host remoto (sin http/https)
* `SG_PORT`: puerto remoto
* `SG_SCHEMA`: protocolo remoto (http/https)