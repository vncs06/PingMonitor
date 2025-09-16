# PingMonitor

Aplicativo simples em C# (WinForms) para testar ping contínuo a até 4 IPs/hosts e exibir o status visualmente:
- Verde quando online
- Vermelho quando offline/erro

Campos:
- 4 abas (Ping 1..4), cada uma com:
	- IP/Host personalizável (ex.: 8.8.8.8, 1.1.1.1)
	- Intervalo em milissegundos
	- Botão Iniciar/Parar
	- Latência (ms)

## Requisitos
- .NET 8 SDK instalado

## Como executar

No PowerShell, dentro da pasta do projeto:

```powershell
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

Ou para gerar o executável self-contained (exemplo x64):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

O executável ficará em `bin/Release/net8.0-windows/win-x64/publish/`.

## Dicas
- Alguns destinos bloqueiam ICMP; “Offline” pode ser firewall.
- Use intervalos maiores para evitar pacotes em excesso (>= 1000 ms).
