# Pruebas de Conexión HTTP para MCP (Streamable HTTP / SSE)

Este documento explica cómo probar la conexión de `McpTester` contra un servidor Model Context Protocol (MCP) remoto a través de HTTP, utilizando el servidor oficial de pruebas **Everything**.

A partir de la versión 1.0.0 del SDK de MCP, el transporte recomendado para conexiones HTTP es **Streamable HTTP**. El antiguo transporte SSE independiente está obsoleto, aunque la especificación moderna sigue usando SSE por debajo de *Streamable HTTP* para recibir mensajes del servidor.

Dado que la arquitectura MCP exige altos estándares de seguridad, **no existen servidores públicos abiertos** en internet (URLs de acceso libre). Por ello, levantar el servidor oficial de forma local es la mejor manera de probar el cliente.

---

## Instrucciones de Uso

Para probar la conexión remota con su aplicación `McpTester`, siga estos pasos:

### 1. Iniciar el Servidor "Everything" Oficial

En lugar de usar un script personalizado, utilizaremos el paquete oficial proporcionado por la especificación de MCP.

Abra una consola **PowerShell** o su **Símbolo del sistema (CMD)** y ejecute:

```powershell
npx -y @modelcontextprotocol/server-everything streamableHttp
```

*(Nota: Mantenga esta consola abierta; si la cierra, el servidor se apaga).*

Esto levantará el servidor "Everything" (que contiene múltiples herramientas de prueba) escuchando en `http://localhost:3001/mcp`.

### 2. Configurar McpTester

El archivo `mcp-servers.json` de su explorador ya debería contar con entradas para probar este servidor.

Debe lucir así:

```json
"Everything-Http": {
  "transport": "streamableHttp",
  "url": "http://localhost:3001/mcp"
}
```

*Nota: También es posible configurar el transporte heredado `sse`, pero requeriría iniciar el servidor especificando ese modo y en otro puerto.*

### 3. Ejecutar y Comprobar

1. Inicie el programa **McpTester**.
2. Haga clic en el botón **"Cargar Configuración"**.
3. El programa leerá el JSON y se conectará automáticamente a la URL usando `HttpClientTransport`.
4. Debería ver en la lista de servidores a `Everything-Http` con múltiples herramientas disponibles (ej: `echo`, `add`, `longRunningOperation`).

Si esto sucede y puede ejecutar herramientas (como `echo`), validará por completo que su infraestructura `.NET` está lista para consumir servidores alojados en la nube (Cloud-based) a través de HTTP en la arquitectura MCP interactuando perfectamente con el estándar.
