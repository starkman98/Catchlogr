import assert from 'node:assert/strict';
import { setTimeout as delay } from 'node:timers/promises';

export const API = 'http://127.0.0.1:15001';
export const WEB = 'https://localhost:17056';
export const MAIL = 'http://127.0.0.1:18025';
export const PACKAGE = 'com.catchlogr.e2e';

export async function jsonRequest(url, options = {}) {
  const response = await fetch(url, { ...options, signal: AbortSignal.timeout(15000) });
  assert(response.ok, `Request rejected with HTTP ${response.status}`);
  return response.json();
}

export async function waitFor(description, action, timeout = 60000) {
  const deadline = Date.now() + timeout;
  while (Date.now() < deadline) {
    const value = await action();
    if (value) return value;
    await delay(750);
  }
  throw new Error(`Timed out waiting for ${description}`);
}

export async function confirmationLink(email) {
  const message = await waitFor('confirmation email', async () => {
    const result = await jsonRequest(`${MAIL}/api/v1/search?query=${encodeURIComponent(`to:${email}`)}`);
    return result.messages?.find(item => item.Subject === 'Confirm your Catchlogr account');
  });
  const detail = await jsonRequest(`${MAIL}/api/v1/message/${message.ID}`);
  const link = detail.Text?.match(/https:\/\/[^\s]+/)?.[0];
  assert(link, 'Email must contain a confirmation link');
  const url = new URL(link);
  assert.equal(url.origin, WEB, 'Confirmation must target this test Web instance');
  assert.equal(url.pathname, '/confirm-email');
  return link;
}

export async function login(email, password) {
  return fetch(`${API}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
    signal: AbortSignal.timeout(15000),
  });
}
