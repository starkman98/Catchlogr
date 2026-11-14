import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { setTimeout as delay } from 'node:timers/promises';
import { join } from 'node:path';
import { chromium } from 'playwright';
import { API, WEB, confirmationLink, jsonRequest, login } from './support.mjs';
import { Android } from './android.mjs';
import { syncFlow } from './sync-flow.mjs';
import { ownershipFlow } from './ownership-flow.mjs';

const backendOnly = process.argv.includes('--backend-only');
const email = `e2e-${randomUUID()}@catchlogr.test`;
const password = `quiet reeds beside water ${randomUUID()}`;
const android = backendOnly ? undefined : new Android();
let browser;
let step = 'start';
let tokens;

async function register() {
  if (!android) {
    const response = await fetch(`${API}/api/auth/register`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }), signal: AbortSignal.timeout(15000),
    });
    assert.equal(response.status, 200, 'Registration must succeed');
    return;
  }
  await android.start();
  await android.waitText('LoginEmailEntry');
  await android.click('OpenRegisterButton');
  await android.fill('RegisterEmailEntry', email);
  await android.fill('RegisterPasswordEntry', password);
  await android.fill('RegisterConfirmPasswordEntry', password);
  await android.click('RegisterButton');
  await android.waitText('ConfirmationEmailEntry');
}

async function rejectUnconfirmedLogin() {
  const rejected = await login(email, password);
  assert.equal(rejected.status, 401, 'Unconfirmed account must not receive tokens');
  if (!android) return;
  await android.click('ConfirmationOpenLoginButton');
  await android.fill('LoginEmailEntry', email);
  await android.fill('LoginPasswordEntry', password);
  await android.click('LoginButton');
  await android.waitText('The email address or password is incorrect.');
}

async function confirmInBrowser() {
  const link = await confirmationLink(email);
  browser = await chromium.launch({ headless: true });
  // Only this disposable browser context accepts the generated local certificate.
  const recordVideo = process.env.E2E_RECORD_VIDEO === '1'
    ? { dir: join(process.env.E2E_ARTIFACTS, 'confirmation-video'), size: { width: 1280, height: 720 } }
    : undefined;
  const context = await browser.newContext({ ignoreHTTPSErrors: true, recordVideo });
  try {
    await context.route('**/*', route => {
      if (new URL(route.request().url()).origin === WEB) return route.continue();
      return route.abort();
    });
    const page = await context.newPage();
    await page.goto(link, { waitUntil: 'domcontentloaded', timeout: 30000 });
    assert.equal(await page.locator('h1').innerText(), 'Email confirmed');
    // Keep the successful page readable in an explicitly requested walkthrough.
    if (recordVideo) await delay(3000);
  } finally {
    await context.close();
  }
}

async function verifyAuthenticatedAccess() {
  const response = await login(email, password);
  assert.equal(response.status, 200, 'Confirmed account must receive tokens');
  tokens = await response.json();
  assert(tokens.accessToken && tokens.refreshToken, 'Real Identity tokens must be issued');
  const trips = await jsonRequest(`${API}/api/fishing-trips`, {
    headers: { Authorization: `Bearer ${tokens.accessToken}` },
  });
  assert.deepEqual(trips, [], 'New account must start with no trips');
  const anonymous = await fetch(`${API}/api/fishing-trips`);
  assert.equal(anonymous.status, 401, 'Anonymous access must remain rejected');
  if (!android) return;
  await android.click('LoginButton');
  await android.waitText('No fishing trips yet.');
}

try {
  for (const [name, action] of [
    ['registration', register],
    ['unconfirmed login rejection', rejectUnconfirmedLogin],
    ['email and Web confirmation', confirmInBrowser],
    ['confirmed login and protected API access', verifyAuthenticatedAccess],
  ]) {
    step = name;
    console.log(`STEP: ${name}`);
    await action();
    console.log(`PASS: ${name}`);
  }
  if (android) {
    step = 'trip/catch sync and offline recovery';
    await syncFlow(android, tokens, email, password);
    step = 'account resource isolation';
    await ownershipFlow(browser, tokens);
  }
  await android?.capture(process.env.E2E_ARTIFACTS).catch(() => {});
  console.log(backendOnly ? 'PASS: backend authentication smoke (Android omitted)' : 'PASS: Android authentication and sync E2E');
} catch (error) {
  // Browser errors can include confirmation URLs; emit no error bodies or stacks.
  console.error(`FAIL: ${step} (${error.name}); account-action URLs and credentials withheld.`);
  // Only local test-file locations; omit error messages, browser stacks and URLs.
  const frames = error.stack?.split('\n').filter(line => /\/tests\/e2e\/[\w.-]+\.mjs:\d+:\d+\)?$/.test(line)) ?? [];
  for (const frame of frames) console.error(frame);
  if (android) await android.capture(process.env.E2E_ARTIFACTS).catch(() => {});
  process.exitCode = 1;
} finally {
  await browser?.close();
  await android?.stop();
}
