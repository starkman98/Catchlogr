import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { API, PACKAGE, waitFor } from './support.mjs';

/** Capture the emulator's virtual scene using the real Android camera intent. */
export async function takePhoto(android) {
  await android.click('TakePhotoButton');
  await waitFor('camera shutter', async () => {
    const source = await android.source();
    if (source.includes('com.android.permissioncontroller:id/permission_allow_foreground_only_button')) {
      await android.click('com.android.permissioncontroller:id/permission_allow_foreground_only_button');
    }
    return source.includes('com.android.camera2:id/shutter_button');
  });
  await android.click('com.android.camera2:id/shutter_button');
  await android.click('com.android.camera2:id/done_button');
  await android.waitText('CatchPhotoPreview');
}

/** Require real private image bytes and reject access without the owner's token. */
export async function verifyPhoto(caught, headers) {
  console.log('  API: verify captured private photo bytes');
  assert(caught.photoUrl, 'Uploaded catch must reference its captured photo');
  const url = new URL(caught.photoUrl, 'http://10.0.2.2:15001');
  assert.equal(url.origin, 'http://10.0.2.2:15001');
  assert.match(url.pathname, /^\/api\/photos\/[a-f0-9-]+$/);
  const response = await fetch(`${API}${url.pathname}`, { headers, signal: AbortSignal.timeout(15000) });
  assert.equal(response.status, 200);
  assert.match(response.headers.get('content-type'), /^image\//);
  const bytes = Buffer.from(await response.arrayBuffer());
  assert(bytes.length > 1000, 'Captured image must contain actual image bytes');
  const anonymous = await fetch(`${API}${url.pathname}`);
  assert.equal(anonymous.status, 401);
  return { path: url.pathname, hash: createHash('sha256').update(bytes).digest('hex') };
}

/** Prove a fresh app cached the authenticated photo rather than only its URL. */
export async function verifyCachedPhoto(hash) {
  const adb = `${process.env.ANDROID_HOME}/platform-tools/adb`;
  const prefix = ['-s', process.env.E2E_DEVICE, 'shell', 'run-as', PACKAGE];
  await waitFor('downloaded private photo bytes', async () => {
    const files = execFileSync(adb, [...prefix, 'find', 'files', '-type', 'f'],
      { encoding: 'utf8', timeout: 10000 }).trim().split('\n').filter(file => file.includes('/photos/'));
    for (const file of files) {
      const digest = execFileSync(adb, [...prefix, 'sha256sum', file], { encoding: 'utf8', timeout: 10000 });
      if (digest.split(/\s+/)[0] === hash) return true;
    }
    return false;
  });
}
