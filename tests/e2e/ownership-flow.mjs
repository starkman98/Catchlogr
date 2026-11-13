import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { API, WEB, confirmationLink, jsonRequest, login } from './support.mjs';

/** Register a second real account and confirm it through the same Web flow. */
async function secondAccount(browser) {
  const email = `other-${randomUUID()}@catchlogr.test`;
  const password = `still water ${randomUUID()}`;
  const response = await fetch(`${API}/api/auth/register`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }), signal: AbortSignal.timeout(15000),
  });
  assert.equal(response.status, 200);
  const context = await browser.newContext({ ignoreHTTPSErrors: true });
  try {
    await context.route('**/*', route => new URL(route.request().url()).origin === WEB
      ? route.continue() : route.abort());
    const page = await context.newPage();
    await page.goto(await confirmationLink(email), { waitUntil: 'domcontentloaded', timeout: 30000 });
    assert.equal(await page.locator('h1').innerText(), 'Email confirmed');
  } finally {
    await context.close();
  }
  const signedIn = await login(email, password);
  assert.equal(signedIn.status, 200);
  return (await signedIn.json()).accessToken;
}

/** Verify another confirmed account cannot read the owner's real saved data. */
export async function ownershipFlow(browser, ownerTokens) {
  console.log('STEP: second-account trip, catch and photo isolation');
  const ownerHeaders = { Authorization: `Bearer ${ownerTokens.accessToken}` };
  const headers = { Authorization: `Bearer ${await secondAccount(browser)}` };
  const trips = await jsonRequest(`${API}/api/fishing-trips`, { headers: ownerHeaders });
  const catches = await jsonRequest(`${API}/api/catches`, { headers: ownerHeaders });
  assert.equal(trips.length, 2);
  assert.equal(catches.length, 2);
  assert.deepEqual(await jsonRequest(`${API}/api/fishing-trips`, { headers }), []);
  assert.deepEqual(await jsonRequest(`${API}/api/catches`, { headers }), []);
  const photo = catches.find(item => item.photoUrl);
  assert(photo, 'Owner must have a real captured private photo');
  for (const path of [
    `/api/fishing-trips/${trips[0].id}`,
    `/api/catches/${catches[0].id}`,
    new URL(photo.photoUrl, API).pathname,
  ]) {
    const response = await fetch(`${API}${path}`, { headers, signal: AbortSignal.timeout(15000) });
    assert.equal(response.status, 404, 'Foreign resource must remain inaccessible');
  }
  console.log('PASS: second-account trip, catch and photo isolation');
}
