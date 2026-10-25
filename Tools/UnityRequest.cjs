// Local MCP-for-Unity framed transport, as implemented by StdioBridgeHost.
// Usage: node Tools/UnityRequest.cjs command '{"parameter":"value"}'
const net = require('node:net');
const socket = net.createConnection({ host: '127.0.0.1', port: Number(process.env.UNITY_MCP_PORT || 6400) });
let bytes = Buffer.alloc(0), welcomed = false, finished = false;
const deadline = setTimeout(() => fail(new Error('Unity request timed out')), 25000);
function fail(error) {
  if (finished) return;
  finished = true;
  clearTimeout(deadline);
  console.error(error.message);
  socket.destroy();
  process.exitCode = 1;
}
socket.on('error', fail);
socket.on('end', () => { if (!finished) fail(new Error('Unity disconnected (possibly compiling); retry after reload.')); });
socket.on('data', chunk => {
  bytes = Buffer.concat([bytes, chunk]);
  if (!welcomed) {
    const end = bytes.indexOf(10);
    if (end < 0) return;
    if (!bytes.subarray(0, end).toString().startsWith('WELCOME UNITY-MCP')) return fail(new Error('Unexpected endpoint'));
    bytes = bytes.subarray(end + 1);
    welcomed = true;
    const body = Buffer.from(JSON.stringify({ type: process.argv[2], params: JSON.parse(process.argv[3] || '{}') }));
    const header = Buffer.alloc(8);
    header.writeBigUInt64BE(BigInt(body.length));
    socket.write(Buffer.concat([header, body]));
  }
  if (bytes.length < 8) return;
  const size = Number(bytes.readBigUInt64BE());
  if (size > 100000000) return fail(new Error('Oversized response'));
  if (bytes.length < size + 8) return;
  finished = true;
  clearTimeout(deadline);
  console.log(bytes.subarray(8, size + 8).toString());
  socket.end();
});
