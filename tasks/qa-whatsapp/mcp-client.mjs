import { spawn } from 'node:child_process';
import { createInterface } from 'node:readline';
import { homedir } from 'node:os';
import { join, resolve } from 'node:path';
import { existsSync } from 'node:fs';

// Transport local sin dependencias: MCP stdio usa un objeto JSON por línea.
export class ChromeMcp {
  constructor({ workspace = process.cwd(), blockWhatsApp = true } = {}) {
    this.workspace = resolve(workspace);
    this.blockWhatsApp = blockWhatsApp;
    this.pending = new Map();
    this.sequence = 0;
    this.stderr = '';
  }

  async start() {
    const entry = process.env.CHROME_DEVTOOLS_MCP_ENTRY || join(homedir(),
      'AppData/Local/npm-cache/_npx/15c61037b1978c83/node_modules/chrome-devtools-mcp/build/src/bin/chrome-devtools-mcp.js');
    if (!existsSync(entry)) throw new Error('Define CHROME_DEVTOOLS_MCP_ENTRY con el entrypoint local de chrome-devtools-mcp.');
    this.child = spawn(process.execPath, [entry,
      '--headless', '--isolated', '--no-usage-statistics', '--no-performance-crux',
      '--no-file-navigations', '--redact-network-headers', '--workspace', this.workspace,
      ...(this.blockWhatsApp ? [
        '--blocked-url-pattern', 'https://wa.me/*',
        '--blocked-url-pattern', 'https://whatsapp.com/*',
        '--blocked-url-pattern', 'https://*.whatsapp.com/*',
      ] : []),
    ], {
      cwd: this.workspace, windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'],
      env: { ...process.env, CHROME_DEVTOOLS_MCP_NO_USAGE_STATISTICS: '1', CHROME_DEVTOOLS_MCP_NO_UPDATE_CHECKS: '1' },
    });
    this.child.stderr.on('data', chunk => { this.stderr = (this.stderr + chunk).slice(-8000); });
    this.child.on('error', error => this.rejectPending(error));
    this.child.on('exit', (code, signal) => this.rejectPending(new Error(`MCP terminó: ${code ?? signal}`)));
    this.lines = createInterface({ input: this.child.stdout });
    this.lines.on('line', line => this.receive(line));
    const initialization = await this.request('initialize', {
      protocolVersion: '2025-03-26', capabilities: { roots: { listChanged: false } },
      clientInfo: { name: 'sabor-latino-qa', version: '1.0.0' },
    });
    this.send({ jsonrpc: '2.0', method: 'notifications/initialized' });
    return initialization;
  }

  send(message) { this.child.stdin.write(JSON.stringify(message) + '\n'); }

  receive(line) {
    let message;
    try { message = JSON.parse(line); } catch { return; }
    if (message.method === 'roots/list' && message.id !== undefined) {
      const uri = new URL('file:///');
      uri.pathname = this.workspace.replaceAll('\\', '/');
      this.send({ jsonrpc: '2.0', id: message.id, result: { roots: [{ uri: uri.href, name: 'SaborLatino QA' }] } });
      return;
    }
    const pending = this.pending.get(message.id);
    if (!pending) return;
    this.pending.delete(message.id);
    clearTimeout(pending.timer);
    if (message.error) pending.reject(new Error(JSON.stringify(message.error)));
    else pending.resolve(message.result);
  }

  rejectPending(error) {
    for (const pending of this.pending.values()) { clearTimeout(pending.timer); pending.reject(error); }
    this.pending.clear();
  }

  request(method, params, timeoutMs = 60000) {
    const id = ++this.sequence;
    return new Promise((resolveRequest, reject) => {
      const timer = setTimeout(() => {
        this.pending.delete(id);
        reject(new Error(`Timeout MCP: ${method}`));
      }, timeoutMs);
      this.pending.set(id, { resolve: resolveRequest, reject, timer });
      this.send({ jsonrpc: '2.0', id, method, params });
    });
  }

  async call(name, args = {}) {
    const result = await this.request('tools/call', { name, arguments: args });
    if (result.isError) throw new Error(`${name}: ${textContent(result)}`);
    return result;
  }

  async close() {
    if (!this.child || this.child.exitCode !== null) return;
    this.child.stdin.end();
    const timer = setTimeout(() => this.child.kill(), 3000);
    await new Promise(resolveClose => this.child.once('exit', resolveClose));
    clearTimeout(timer);
    this.lines.close();
  }
}

export function textContent(result) {
  return (result.content ?? []).filter(block => block.type === 'text').map(block => block.text).join('\n');
}
