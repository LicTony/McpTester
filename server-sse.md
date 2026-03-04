# Servidor Mock SSE para Pruebas del Protocolo MCP

Este repositorio incluye un script de prueba (`server-sse.js`) diseñado para simular el comportamiento de un servidor Model Context Protocol (MCP) remoto, que se comunica a través de **Server-Sent Events (SSE)** mediante HTTP.

Dado que la arquitectura MCP exige altos estándares de seguridad y uso controlado de herramientas, **no existen servidores públicos abiertos** en internet (URLs de acceso libre). Por ello, levantar un servidor web "falso" (*mock server*) localmente es la mejor manera de probar que su cliente `McpTester` soporta correctamente el protocolo de conexión `sse`.

## ¿Para qué sirve?

1. **Verificar el Transporte de Red**: Garantizar que la lógica de conexión a través de `http://` en lugar de una consola (stdio) funcione.
2. **Probar Eventos Síncronos**: Asegurar que su aplicación pueda mantener una conexión HTTP ininterrumpidamente (`Keep-Alive`) y reciba los flujos de "datos empujados" (*pushed data*) a medida que el servidor los genera en tiempo real.
3. **Comprobar la Autenticación/Cabeceras (Opcional)**: En el futuro, si MCP requiere *tokens*, usar un servidor simulado permite emular bloqueos de seguridad como los clásicos Errores `401 Unauthorized`.

---

## Instrucciones de Uso

Para probar este servidor con su aplicación principal `McpTester`, siga estos pasos:

### 1. Iniciar el Servidor de Prueba

Primero debe levantar este servidor web independiente antes de arrancar su aplicación.

Abra una consola **PowerShell** o su **Símbolo del sistema (CMD)** en esta misma carpeta y ejecute:

```powershell
node server-sse.js
```

Debería observar un mensaje de éxito indicando que el servidor escucha en `http://localhost:3000/sse`.

*(Nota: Mantenga esta consola abierta; si la cierra, el servidor "remoto" se apaga).*

### 2. Configurar McpTester

El archivo `mcp-servers.json` de su explorador ya debería contar con una entrada llamada `EjemploRemotoHTTP`, la cual indica que utilice la red para buscar el proceso en lugar de comandos locales.

Debe lucir así:

```json
"EjemploRemotoHTTP": {
  "transport": "sse",
  "url": "http://localhost:3000/sse"
}
```

### 3. Ejecutar y Comprobar

Inicie el **McpTester**. Vaya al botón **"Cargar Configuración"** (Load Config).

- El programa leerá el JSON y detectará el transporte híbrido.
- La aplicación se conectará automáticamente a la URL. Si usted mira la consola negra donde corrió Node, verá un registro notificándole: `¡Cliente conectado a la transmisión SSE!`.
- Si esto último sucede, validará por completo que su infraestructura `.NET` está lista para consumir servidores de terceros o Cloud-based en la arquitectura MCP.
