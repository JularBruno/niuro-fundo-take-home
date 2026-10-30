// Mock external service: receives customers + applications from the loan API's background worker.
// In-memory only, zero dependencies.
//
//   POST /customers              create (idempotent on customer.id, so a retried create never duplicates)
//   PUT  /customers/:id          update
//   GET  /customers              everything received so far (for the demo)
//   POST /simulate-outage/:n     fail the next n create/update requests with 503 (to demo retries); 0 cancels
import http from 'node:http';

const PORT = Number(process.env.PORT ?? 4000);
const customers = new Map();
let failuresLeft = 0;

const maskSsn = (ssn = '') => `***-**-${String(ssn).slice(-4)}`;
const time = () => new Date().toISOString().slice(11, 19);
const log = (message) => console.log(`[${time()}] ${message}`);
const describe = ({ customer, application }) =>
  `${customer.firstName} ${customer.lastName} (${maskSsn(customer.ssn)}), ${customer.address?.state}, $${application.requestedAmount}`;

function send(res, status, body) {
  res.writeHead(status, { 'Content-Type': 'application/json' });
  res.end(body === undefined ? '' : JSON.stringify(body, null, 2));
}

function readJson(req) {
  return new Promise((resolve, reject) => {
    let raw = '';
    req.on('data', (chunk) => (raw += chunk));
    req.on('end', () => {
      try {
        const body = JSON.parse(raw);
        if (!body?.customer?.id || !body?.application?.id) throw new Error('expected { customer, application }');
        resolve(body);
      } catch (error) {
        reject(error);
      }
    });
  });
}

const server = http.createServer(async (req, res) => {
  const { pathname } = new URL(req.url, `http://${req.headers.host}`);
  const [, resource, id] = pathname.split('/');

  if (req.method === 'GET' && pathname === '/customers') {
    return send(res, 200, [...customers.values()]);
  }

  if (req.method === 'POST' && resource === 'simulate-outage') {
    failuresLeft = Math.max(0, Number(id) || 0); // 0 cancels a pending outage
    log(failuresLeft ? `⚠ next ${failuresLeft} request(s) will fail with 503` : '✓ simulated outage cancelled');
    return send(res, 200, { failuresLeft });
  }

  const isCreate = req.method === 'POST' && pathname === '/customers';
  const isUpdate = req.method === 'PUT' && resource === 'customers' && id;
  if (!isCreate && !isUpdate) return send(res, 404, { error: 'not found' });

  if (failuresLeft > 0) {
    failuresLeft--;
    log(`✗ 503 ${req.method} ${pathname} (simulated outage, ${failuresLeft} left)`);
    return send(res, 503, { error: 'simulated outage' });
  }

  let body;
  try {
    body = await readJson(req);
  } catch (error) {
    return send(res, 400, { error: error.message });
  }

  const now = new Date().toISOString();
  const customerId = isCreate ? body.customer.id : id;
  const existing = customers.get(customerId);

  if (isCreate && existing) {
    log(`= DUPLICATE CREATE ignored ${customerId} (already received — likely a retry)`);
    return send(res, 200, existing);
  }

  const record = {
    customer: body.customer,
    application: body.application,
    createdAt: existing?.createdAt ?? now,
    updatedAt: now,
    updates: isUpdate ? (existing?.updates ?? 0) + 1 : 0,
  };
  customers.set(customerId, record);

  if (isCreate) log(`+ CREATE ${customerId} → ${describe(body)}`);
  else log(`~ UPDATE ${customerId} → ${describe(body)}${existing ? '' : ' (was unknown, stored)'}`);

  return send(res, 200, record);
});

server.listen(PORT, () => log(`Mock external service listening on http://localhost:${PORT}`));
