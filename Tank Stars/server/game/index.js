const { WebSocketServer } = require('ws');
const wss = new WebSocketServer({ port: 3002 });

wss.on('connection', (ws) => {
  console.log('Client connected');
  ws.send(JSON.stringify({ event: 'connected', message: 'Game Service running' }));
});

console.log('Game Service running on port 3002');