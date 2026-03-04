const http = require('http');

// ─── Definición de tools mockeadas ───────────────────────────────────────────
// Acá definís las herramientas que tu servidor "expone" al cliente.
// Cada tool tiene: nombre, descripción, y el schema de sus parámetros (JSON Schema).
const TOOLS = [
    {
        name: "obtener_clima",
        description: "Devuelve el clima actual de una ciudad",
        inputSchema: {
            type: "object",
            properties: {
                ciudad: {
                    type: "string",
                    description: "Nombre de la ciudad"
                }
            },
            required: ["ciudad"]
        }
    },
    {
        name: "sumar_numeros",
        description: "Suma dos números enteros",
        inputSchema: {
            type: "object",
            properties: {
                a: { type: "number", description: "Primer número" },
                b: { type: "number", description: "Segundo número" }
            },
            required: ["a", "b"]
        }
    }
];

// ─── Lógica mockeada de cada tool ─────────────────────────────────────────────
// Esta función simula la ejecución de una herramienta.
// En un servidor real, acá llamarías a una API, base de datos, etc.
function ejecutarTool(nombre, args) {
    if (nombre === "obtener_clima") {
        return {
            content: [
                {
                    type: "text",
                    text: `El clima en ${args.ciudad} es soleado, 22°C. (dato mockeado)`
                }
            ]
        };
    }

    if (nombre === "sumar_numeros") {
        const resultado = args.a + args.b;
        return {
            content: [
                {
                    type: "text",
                    text: `La suma de ${args.a} + ${args.b} = ${resultado}`
                }
            ]
        };
    }

    // Si la tool no existe, devolvemos un error MCP
    return {
        isError: true,
        content: [
            {
                type: "text",
                text: `Tool desconocida: ${nombre}`
            }
        ]
    };
}

// ─── Mapa de conexiones SSE activas ──────────────────────────────────────────
// Usamos un Map para soportar múltiples clientes simultáneos.
// La clave es un ID único por conexión.
const sseConnections = new Map();
let nextConnectionId = 1;

// ─── Servidor HTTP ────────────────────────────────────────────────────────────
const server = http.createServer((req, res) => {
    console.log(`\n=== [${new Date().toISOString()}] ${req.method} ${req.url} ===`);

    // CORS: necesario si tu cliente corre en un origen diferente (ej: browser)
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', '*');

    if (req.method === 'OPTIONS') {
        res.writeHead(204);
        res.end();
        return;
    }

    // ── Canal SSE: el cliente abre esta conexión y la mantiene abierta ──────
    if (req.url.startsWith('/sse') && req.method === 'GET') {
        const connectionId = nextConnectionId++;
        console.log(`✓ Nueva conexión SSE. ID: ${connectionId}`);

        res.writeHead(200, {
            'Content-Type': 'text/event-stream',
            'Cache-Control': 'no-cache',
            'Connection': 'keep-alive'
        });

		// Guardamos la conexión en el mapa
		sseConnections.set(connectionId, res);

		// Detectamos errores en el stream
		res.on('error', (err) => {
			console.error(`✗ Error en stream SSE cliente ${connectionId}:`, err);
		});

		// Verificamos que cada write realmente se envió (retorna false si el buffer está lleno)
		const written1 = res.write('event: endpoint\n');
		console.log(`  > write 'event: endpoint\\n' resultado: ${written1}`);

		const written2 = res.write(`data: http://localhost:3000/messages?clientId=${connectionId}\n\n`);
		console.log(`  > write 'data: url\\n\\n' resultado: ${written2}`);

		console.log(`✓ 'endpoint' enviado al cliente ${connectionId}`);

       

        // Cuando el cliente cierra la conexión, la limpiamos del mapa
        req.on('close', () => {
            sseConnections.delete(connectionId);
            console.log(`✗ SSE cerrado por cliente ${connectionId}`);
        });

        return;
    }

    // ── Endpoint de mensajes: recibe los JSON-RPC del cliente ───────────────
    if (req.url.startsWith('/messages') && req.method === 'POST') {
        // Extraemos el clientId del query string para saber a qué SSE responder
        const urlObj = new URL(req.url, 'http://localhost:3000');
        const clientId = parseInt(urlObj.searchParams.get('clientId'));
        const sseRes = sseConnections.get(clientId);

        let body = '';
        req.on('data', chunk => { body += chunk.toString(); });
        req.on('end', () => {
            console.log(`<<< POST de cliente ${clientId}:`, body);
            try {
                const msg = body ? JSON.parse(body) : null;

                // Siempre respondemos 202 al POST (sin body).
                // La respuesta real llega por el canal SSE.
                res.writeHead(202, { 'Content-Type': 'text/plain' });
                res.end();
                console.log("✓ 202 enviado");

                // Si es una notificación (sin id), no respondemos nada
                if (!msg || msg.id === undefined) {
                    console.log(`ℹ Notificación recibida (${msg?.method}), sin respuesta.`);
                    return;
                }

                // Si no hay canal SSE activo para este cliente, no podemos responder
                if (!sseRes) {
                    console.warn(`⚠ No hay SSE activo para clientId ${clientId}`);
                    return;
                }

                // ── Procesamos el método JSON-RPC ─────────────────────────
                let responseToClient = null;

                if (msg.method === 'initialize') {
                   
				   responseToClient = {
						jsonrpc: "2.0",
						id: msg.id,
						result: {
							protocolVersion: "2024-11-05",
							capabilities: {
								tools: {}
							},
							serverInfo: {
								name: "MockMCP",
								version: "1.0.0"
							}
						}
					};

					// Después de enviar el resultado del initialize,
					// el servidor TAMBIÉN debe notificar que está listo
					const notificacion = {
						jsonrpc: "2.0",
						method: "notifications/initialized"
						// Sin "id" porque es una notificación, no una request
					};

					// Enviamos primero la respuesta al initialize
					const evt1 = `event: message\ndata: ${JSON.stringify(responseToClient)}\n\n`;
					sseRes.write(evt1);
					console.log(`=> SSE: respuesta initialize enviada al cliente ${clientId}`);

					// Luego enviamos la notificación
					const evt2 = `event: message\ndata: ${JSON.stringify(notificacion)}\n\n`;
					sseRes.write(evt2);
					console.log(`=> SSE: notifications/initialized enviada al cliente ${clientId}`);

					// Ponemos null para que el código de abajo no lo envíe de nuevo
					responseToClient = null;
				   

                } else if (msg.method === 'tools/list') {
                    // Devolvemos la lista de tools definidas arriba
                    responseToClient = {
                        jsonrpc: "2.0",
                        id: msg.id,
                        result: { tools: TOOLS }
                    };

                } else if (msg.method === 'tools/call') {
                    // Ejecutamos la tool solicitada
                    const toolName = msg.params?.name;
                    const toolArgs = msg.params?.arguments || {};
                    console.log(`🔧 Ejecutando tool: ${toolName}`, toolArgs);

                    const toolResult = ejecutarTool(toolName, toolArgs);
                    responseToClient = {
                        jsonrpc: "2.0",
                        id: msg.id,
                        result: toolResult
                    };

                } else if (msg.method === 'ping') {
                    // Algunos clientes envían ping para verificar que el servidor sigue vivo
                    responseToClient = {
                        jsonrpc: "2.0",
                        id: msg.id,
                        result: {}
                    };

                } else {
                    // Método desconocido: respondemos con error JSON-RPC estándar
                    console.warn(`⚠ Método desconocido: ${msg.method}`);
                    responseToClient = {
                        jsonrpc: "2.0",
                        id: msg.id,
                        error: {
                            code: -32601,
                            message: `Método no soportado: ${msg.method}`
                        }
                    };
                }

                // ── Enviamos la respuesta por el canal SSE ─────────────────
                if (responseToClient) {
                    const evt = `event: message\ndata: ${JSON.stringify(responseToClient)}\n\n`;
                    console.log(`=> SSE evento enviado al cliente ${clientId}`);
                    sseRes.write(evt);
                }

            } catch (e) {
                console.error("Error procesando mensaje:", e);
                res.writeHead(500);
                res.end("Error interno");
            }
        });
        return;
    }

    // ── Ruta no encontrada ──────────────────────────────────────────────────
    res.writeHead(404);
    res.end("Not Found");
    console.log("✗ 404");
});

server.listen(3000, () => {
    console.log("🚀 Mock MCP SSE escuchando en http://localhost:3000");
    console.log("   Conectate via SSE a: http://localhost:3000/sse");
});
