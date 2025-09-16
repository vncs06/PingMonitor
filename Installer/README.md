# PingMonitor Installer (WiX v3 + Burn)

Distribuição adotada: Bundle-only
- Entregamos apenas o Bundle (bootstrapper .exe) que instala o .NET Desktop Runtime 8 x64 (se necessário) e executa o MSI embutido.
- O MSI é artefato temporário de build; não é distribuído.
- O instalador pergunta se deseja criar atalho na Área de Trabalho.

Pré-requisitos:
- .NET SDK 8 instalado (para publicar o app)
- WiX Toolset v3.11 (defina a variável de ambiente `WIX` para a pasta `bin`, ex.: `C:\Program Files (x86)\WiX Toolset v3.11\bin`)

Build:
```powershell
cd "c:\Users\gabriel.vinicius\Desktop\Programas\PingMonitor\Installer"
./build.ps1 -Configuration Release -Runtime win-x64
```
Saída:
- Bundle: `Installer\Bundle\PingMonitorSetup.exe`

Observações:
- O bundle baixa o runtime do .NET 8 Desktop x64 se não houver (link no `Bundle.wxs`).
- O MSI usa Publish Single File (self-contained), então não depende do .NET instalado para rodar.
- O diálogo de atalho aparece logo após escolher a pasta de instalação.

Distribuição:
- Entregue apenas o `PingMonitorSetup.exe` (Bundle). Ele já EMBUTE o `PingMonitor.msi` (`Compressed="yes"`) e o runtime necessário.
	O MSI não é publicado nem versionado separadamente neste fluxo.

Layout de build/publish padronizado:
- Todos os artefatos de publicação ficam em `bin/<Config>/net8.0-windows/<RID>/publish/`.
- O script `build.ps1` limpa e copia o conteúdo do `publish/` para `Installer/Msi/Payload/` antes de compilar o MSI.
- Se você publicar para outro RID (ex.: `win-arm64`), passe `-Runtime win-arm64` e o script usará a pasta correspondente.