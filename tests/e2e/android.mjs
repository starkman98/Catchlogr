import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
import { PACKAGE, waitFor } from './support.mjs';

const SERVER = 'http://127.0.0.1:4723';
const ELEMENT = 'element-6066-11e4-a52e-4f735466cecf';

/** Drives only the disposable E2E package on the explicitly selected emulator. */
export class Android {
  async command(method, path, data) {
    const response = await fetch(`${SERVER}${path}`, {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: data === undefined ? undefined : JSON.stringify(data),
      signal: AbortSignal.timeout(180000),
    });
    const result = await response.json();
    // WebDriver errors may echo entered credentials. Never propagate their bodies.
    assert(response.ok && !result.value?.error, `Appium command failed: ${method} ${path.split('/').at(-1)}`);
    return result.value;
  }

  async start() {
    const result = await this.command('POST', '/session', {
      capabilities: { alwaysMatch: {
        platformName: 'Android',
        'appium:automationName': 'UiAutomator2',
        'appium:udid': process.env.E2E_DEVICE ?? 'emulator-5554',
        'appium:appPackage': PACKAGE,
        'appium:appActivity': process.env.E2E_ACTIVITY,
        'appium:noReset': true,
        'appium:autoGrantPermissions': true,
        'appium:forceAppLaunch': true,
        'appium:newCommandTimeout': 300,
        'appium:adbExecTimeout': 120000,
        'appium:uiautomator2ServerInstallTimeout': 120000,
        'appium:uiautomator2ServerLaunchTimeout': 120000,
      } },
    });
    this.prefix = `/session/${result.sessionId}`;
  }

  async source() { return this.command('GET', `${this.prefix}/source`); }

  async waitText(text) {
    return waitFor(`UI text ${text}`, async () => (await this.source()).includes(text));
  }

  async element(id) {
    return waitFor(`control ${id}`, async () => {
      const matches = await this.command('POST', `${this.prefix}/elements`, {
        using: 'xpath',
        value: `//*[@resource-id="${id}" or @resource-id="${PACKAGE}:id/${id}" or @content-desc="${id}"]`,
      });
      return matches[0]?.[ELEMENT];
    });
  }

  async click(id) {
    const element = await this.element(id);
    await this.command('POST', `${this.prefix}/element/${element}/click`, {});
  }

  async fill(id, text) {
    const element = await this.element(id);
    await this.command('POST', `${this.prefix}/element/${element}/clear`, {});
    await this.command('POST', `${this.prefix}/element/${element}/value`, { text });
    // Android Back dismisses the keyboard only when it is shown.
    const shown = await this.command('GET', `${this.prefix}/appium/device/is_keyboard_shown`);
    if (shown) await this.command('POST', `${this.prefix}/back`, {});
  }

  async back() {
    const element = await this.command('POST', `${this.prefix}/element`, {
      using: 'accessibility id', value: 'Navigate up',
    });
    await this.command('POST', `${this.prefix}/element/${element[ELEMENT]}/click`, {});
  }

  async tapText(text) {
    const element = await waitFor('visible row', async () => {
      const matches = await this.command('POST', `${this.prefix}/elements`, {
        using: 'xpath', value: `//*[@text="${text}"]`,
      });
      return matches[0]?.[ELEMENT];
    });
    await this.command('POST', `${this.prefix}/element/${element}/click`, {});
  }

  async scrollTo(id) {
    const visible = await this.command('POST', `${this.prefix}/elements`, {
      using: 'xpath', value: `//*[@resource-id="${PACKAGE}:id/${id}" and @displayed="true"]`,
    });
    if (visible.length) return;
    await this.command('POST', `${this.prefix}/element`, {
      using: '-android uiautomator',
      value: `new UiScrollable(new UiSelector().scrollable(true)).scrollIntoView(new UiSelector().resourceId("${PACKAGE}:id/${id}"))`,
    });
  }

  async refresh() {
    await this.command('POST', `${this.prefix}/execute/sync`, {
      script: 'mobile: swipeGesture',
      args: [{ left: 150, top: 600, width: 750, height: 1200, direction: 'down', percent: 0.8 }],
    });
  }

  async capture(directory) {
    if (!this.prefix) return;
    await mkdir(directory, { recursive: true, mode: 0o700 });
    const source = await this.source();
    await writeFile(`${directory}/ui.xml`, source.replace(/text="[^"]*"/g, 'text="[redacted]"'), { mode: 0o600 });
    // Screenshot capture has been unreliable on this host; hierarchy remains useful.
    try {
      const png = await this.command('GET', `${this.prefix}/screenshot`);
      await writeFile(`${directory}/screen.png`, Buffer.from(png, 'base64'), { mode: 0o600 });
    } catch { console.warn('Screenshot unavailable; redacted hierarchy retained.'); }
  }

  async stop() {
    if (this.prefix) await this.command('DELETE', this.prefix);
  }
}
