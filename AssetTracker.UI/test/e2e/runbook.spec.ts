import * as dotenv from 'dotenv';
import { APIRequestContext, expect, request, test } from '@playwright/test';

// Load .env.runbook before any env var reads
dotenv.config({ path: '.env.runbook' });

type ApiResponse<T = any> = {
  status: number;
  ok: boolean;
  body: T | null;
  text: string;
};

type UserSummary = {
  userID: number;
  fullName: string;
  email: string;
  accountStatus: string;
  rejectionReason?: string;
};

type Asset = {
  assetID: number;
  name: string;
  category: string;
  serialNumber: string;
  status: 'Available' | 'Checked Out' | 'Under Maintenance' | 'Decommissioned';
  assignedTo: string;
  purchaseDate: string;
  value: number;
};

type RunContext = {
  api: APIRequestContext;
  adminToken: string;
  stamp: number;
  createdAssets: number[];
  decommissionedAssets: number[];
  approvedUsers: Array<{ email: string; password: string; fullName: string }>;
};

const PASSWORD = process.env.RUNBOOK_TEST_PASSWORD ?? 'Pass!2026A';
const ADMIN_EMAIL = process.env.RUNBOOK_ADMIN_EMAIL;
const ADMIN_PASSWORD = process.env.RUNBOOK_ADMIN_PASSWORD;
const ADMIN_TOKEN = process.env.RUNBOOK_ADMIN_TOKEN;

async function callApi(
  api: APIRequestContext,
  path: string,
  options?: {
    method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
    token?: string;
    data?: any;
    params?: Record<string, string>;
  }
): Promise<ApiResponse> {
  const method = options?.method ?? 'GET';
  const headers: Record<string, string> = {};
  if (options?.token) {
    headers.Authorization = `Bearer ${options.token}`;
  }

  const resp = await api.fetch(path, {
    method,
    headers,
    data: options?.data,
    params: options?.params,
  });

  const text = await resp.text();
  let body: any = null;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    body = null;
  }

  return {
    status: resp.status(),
    ok: resp.ok(),
    body,
    text,
  };
}

async function login(api: APIRequestContext, email: string, password: string): Promise<ApiResponse<{ token?: string }>> {
  return callApi(api, '/api/auth/login', {
    method: 'POST',
    data: { email, password },
  });
}

async function registerUser(ctx: RunContext, prefix: string) {
  const email = `${prefix}.${ctx.stamp}.${Math.floor(Math.random() * 100000)}@example.test`;
  const employeeID = `${prefix.toUpperCase()}${ctx.stamp}${Math.floor(Math.random() * 1000)}`;
  const fullName = `${prefix.toUpperCase()} User ${ctx.stamp}`;

  const registration = await callApi(ctx.api, '/api/auth/register', {
    method: 'POST',
    data: {
      fullName,
      email,
      employeeID,
      department: 'QA',
      password: PASSWORD,
      phoneNumber: '0123456789',
    },
  });

  return { registration, email, employeeID, fullName };
}

async function getUsers(ctx: RunContext): Promise<UserSummary[]> {
  const usersRes = await callApi(ctx.api, '/api/users', {
    token: ctx.adminToken,
  });
  expect(usersRes.ok, `Expected /api/users success, got ${usersRes.status} ${usersRes.text}`).toBeTruthy();
  return (usersRes.body as UserSummary[]) ?? [];
}

async function findUserByEmail(ctx: RunContext, email: string): Promise<UserSummary | undefined> {
  const users = await getUsers(ctx);
  return users.find((u) => u.email.toLowerCase() === email.toLowerCase());
}

async function approveUser(ctx: RunContext, userId: number) {
  return callApi(ctx.api, `/api/users/${userId}/approve`, {
    method: 'POST',
    token: ctx.adminToken,
    data: {},
  });
}

async function rejectUser(ctx: RunContext, userId: number, rejectionReason: string) {
  return callApi(ctx.api, `/api/users/${userId}/reject`, {
    method: 'POST',
    token: ctx.adminToken,
    data: { rejectionReason },
  });
}

async function getAssets(ctx: RunContext): Promise<Asset[]> {
  const assetsRes = await callApi(ctx.api, '/api/assets', {
    token: ctx.adminToken,
  });
  expect(assetsRes.ok, `Expected /api/assets success, got ${assetsRes.status} ${assetsRes.text}`).toBeTruthy();
  return (assetsRes.body as Asset[]) ?? [];
}

async function createAsset(
  ctx: RunContext,
  namePrefix: string,
  status: Asset['status'] = 'Available'
): Promise<Asset> {
  const serial = `${namePrefix}-${ctx.stamp}-${Math.floor(Math.random() * 10000)}`.toUpperCase();
  const createRes = await callApi(ctx.api, '/api/assets', {
    method: 'POST',
    token: ctx.adminToken,
    data: {
      name: `${namePrefix}-${ctx.stamp}`,
      category: 'Laptop',
      serialNumber: serial,
      value: 1000,
      status: 'Available',
    },
  });

  expect(createRes.ok, `Asset create failed: ${createRes.status} ${createRes.text}`).toBeTruthy();

  const created = createRes.body as Asset;
  ctx.createdAssets.push(created.assetID);

  if (status === 'Under Maintenance') {
    const setMaint = await callApi(ctx.api, `/api/assets/${created.assetID}/maintenance`, {
      method: 'PUT',
      token: ctx.adminToken,
      data: { setMaintenance: true },
    });
    expect(setMaint.ok, `Maintenance set failed: ${setMaint.status} ${setMaint.text}`).toBeTruthy();
  }

  if (status === 'Decommissioned') {
    const decom = await callApi(ctx.api, `/api/assets/${created.assetID}`, {
      method: 'DELETE',
      token: ctx.adminToken,
    });
    expect(decom.ok, `Decommission failed: ${decom.status} ${decom.text}`).toBeTruthy();
    ctx.decommissionedAssets.push(created.assetID);
  }

  const refreshed = (await getAssets(ctx)).find((a) => a.assetID === created.assetID);
  expect(refreshed).toBeTruthy();
  return refreshed as Asset;
}

async function assignAsset(
  ctx: RunContext,
  payload: {
    assetID: number;
    employeeEmail: string;
    employeeFullName: string;
    expectedReturnDate: string;
  }
) {
  return callApi(ctx.api, '/api/checkouts/assign', {
    method: 'POST',
    token: ctx.adminToken,
    data: {
      ...payload,
      durationDays: 2,
    },
  });
}

function isoDateDaysFromNow(days: number): string {
  return new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);
}

test.describe.serial('Assessment Runbook API Suite', () => {
  let ctx: RunContext;

  test.beforeAll(async () => {
    const api = await request.newContext({
      baseURL: process.env.API_BASE_URL ?? 'https://localhost:7175',
      ignoreHTTPSErrors: true,
    });

    let adminToken = ADMIN_TOKEN;

    if (!adminToken) {
      if (!ADMIN_EMAIL || !ADMIN_PASSWORD) {
        throw new Error('Set RUNBOOK_ADMIN_TOKEN or both RUNBOOK_ADMIN_EMAIL and RUNBOOK_ADMIN_PASSWORD before running this suite.');
      }

      const loginRes = await login(api, ADMIN_EMAIL, ADMIN_PASSWORD);
      if (!loginRes.ok || !loginRes.body?.token) {
        throw new Error(`Admin login failed: ${loginRes.status} ${loginRes.text}`);
      }

      adminToken = loginRes.body.token;
    }

    ctx = {
      api,
      adminToken: adminToken as string,
      stamp: Date.now(),
      createdAssets: [],
      decommissionedAssets: [],
      approvedUsers: [],
    };
  });

  test.afterAll(async () => {
    if (!ctx) return;

    for (const user of ctx.approvedUsers) {
      const loginRes = await login(ctx.api, user.email, user.password);
      const userToken = loginRes.body?.token;
      if (!userToken) continue;

      const myAssetsRes = await callApi(ctx.api, '/api/checkouts/my-assets', { token: userToken });
      const myAssets = (myAssetsRes.body as Array<{ assetID: number }>) ?? [];
      for (const asset of myAssets) {
        await callApi(ctx.api, `/api/assets/${asset.assetID}/admin-return`, {
          method: 'POST',
          token: ctx.adminToken,
          data: {},
        });
      }
    }

    for (const assetId of ctx.decommissionedAssets) {
      await callApi(ctx.api, `/api/assets/${assetId}/restore`, {
        method: 'POST',
        token: ctx.adminToken,
        data: {},
      });
    }

    await ctx.api.dispose();
  });

  test('1) Registration creates Pending user and blocks login', async () => {
    const u = await registerUser(ctx, 'pending');
    expect(u.registration.ok).toBeTruthy();

    const row = await findUserByEmail(ctx, u.email);
    expect(row).toBeTruthy();
    expect(row?.accountStatus).toBe('Pending');

    const loginPending = await login(ctx.api, u.email, PASSWORD);
    expect(loginPending.ok).toBeFalsy();
    expect([401, 403]).toContain(loginPending.status);
  });

  test('2) Approval + rejection with reason + audit rows', async () => {
    const approveCandidate = await registerUser(ctx, 'approve');
    const rejectCandidate = await registerUser(ctx, 'reject');

    const approveRow = await findUserByEmail(ctx, approveCandidate.email);
    const rejectRow = await findUserByEmail(ctx, rejectCandidate.email);
    expect(approveRow).toBeTruthy();
    expect(rejectRow).toBeTruthy();

    const approveRes = await approveUser(ctx, (approveRow as UserSummary).userID);
    expect(approveRes.ok).toBeTruthy();

    const rejectNoReason = await rejectUser(ctx, (rejectRow as UserSummary).userID, '');
    expect(rejectNoReason.ok).toBeFalsy();
    expect(rejectNoReason.status).toBe(400);

    const rejectWithReason = await rejectUser(ctx, (rejectRow as UserSummary).userID, 'Insufficient onboarding docs');
    expect(rejectWithReason.ok).toBeTruthy();

    const approvedLogin = await login(ctx.api, approveCandidate.email, PASSWORD);
    expect(approvedLogin.ok).toBeTruthy();

    const afterApprove = await findUserByEmail(ctx, approveCandidate.email);
    const afterReject = await findUserByEmail(ctx, rejectCandidate.email);
    expect(afterApprove?.accountStatus).toBe('Active');
    expect(afterReject?.accountStatus).toBe('Rejected');
    expect(Boolean(afterReject?.rejectionReason)).toBeTruthy();

    const today = new Date().toISOString().slice(0, 10);
    const approveAudit = await callApi(ctx.api, '/api/checkouts/audit-trail', {
      token: ctx.adminToken,
      params: { action: 'APPROVE_USER', fromDate: today, toDate: today },
    });
    const rejectAudit = await callApi(ctx.api, '/api/checkouts/audit-trail', {
      token: ctx.adminToken,
      params: { action: 'REJECT_USER', fromDate: today, toDate: today },
    });

    expect(approveAudit.ok).toBeTruthy();
    expect(rejectAudit.ok).toBeTruthy();
    expect(((approveAudit.body as any[]) ?? []).length).toBeGreaterThan(0);
    expect(((rejectAudit.body as any[]) ?? []).length).toBeGreaterThan(0);
  });

  test('3) Successful checkout updates status and stores user snapshot', async () => {
    const std = await registerUser(ctx, 'checkout');
    const stdRow = await findUserByEmail(ctx, std.email);
    expect(stdRow).toBeTruthy();

    const approveRes = await approveUser(ctx, (stdRow as UserSummary).userID);
    expect(approveRes.ok).toBeTruthy();

    ctx.approvedUsers.push({ email: std.email, password: PASSWORD, fullName: std.fullName });

    const available = (await getAssets(ctx)).find((a) => a.status === 'Available') ?? (await createAsset(ctx, 'avail', 'Available'));

    const assignRes = await assignAsset(ctx, {
      assetID: available.assetID,
      employeeEmail: std.email,
      employeeFullName: std.fullName,
      expectedReturnDate: isoDateDaysFromNow(2),
    });
    expect(assignRes.ok).toBeTruthy();

    const updated = (await getAssets(ctx)).find((a) => a.assetID === available.assetID);
    expect(updated?.status).toBe('Checked Out');

    const report = await callApi(ctx.api, '/api/reports/checkout-history', {
      token: ctx.adminToken,
      params: { assetId: String(available.assetID) },
    });
    expect(report.ok).toBeTruthy();

    const row = ((report.body as any[]) ?? [])[0];
    expect(row).toBeTruthy();
    expect(String(row.userEmail).toLowerCase()).toBe(std.email.toLowerCase());
    expect(String(row.userFullName)).toContain(std.fullName.split(' ')[0]);
  });

  test('4) Checkout validation gate rejects illegal cases', async () => {
    const std = await registerUser(ctx, 'gate');
    const stdRow = await findUserByEmail(ctx, std.email);
    expect(stdRow).toBeTruthy();

    const approveRes = await approveUser(ctx, (stdRow as UserSummary).userID);
    expect(approveRes.ok).toBeTruthy();

    const available = (await getAssets(ctx)).find((a) => a.status === 'Available') ?? (await createAsset(ctx, 'gate-avail', 'Available'));
    const maint = (await getAssets(ctx)).find((a) => a.status === 'Under Maintenance') ?? (await createAsset(ctx, 'gate-maint', 'Under Maintenance'));
    const decomm = (await getAssets(ctx)).find((a) => a.status === 'Decommissioned') ?? (await createAsset(ctx, 'gate-decom', 'Decommissioned'));

    const checkedAssign = await assignAsset(ctx, {
      assetID: available.assetID,
      employeeEmail: std.email,
      employeeFullName: std.fullName,
      expectedReturnDate: isoDateDaysFromNow(2),
    });
    expect(checkedAssign.ok).toBeTruthy();

    const noAsset = await assignAsset(ctx, {
      assetID: 0,
      employeeEmail: std.email,
      employeeFullName: std.fullName,
      expectedReturnDate: isoDateDaysFromNow(2),
    });
    expect(noAsset.ok).toBeFalsy();

    const underMaintenance = await assignAsset(ctx, {
      assetID: maint.assetID,
      employeeEmail: std.email,
      employeeFullName: std.fullName,
      expectedReturnDate: isoDateDaysFromNow(2),
    });
    expect(underMaintenance.ok).toBeFalsy();

    const checkedOut = await assignAsset(ctx, {
      assetID: available.assetID,
      employeeEmail: std.email,
      employeeFullName: std.fullName,
      expectedReturnDate: isoDateDaysFromNow(2),
    });
    expect(checkedOut.ok).toBeFalsy();

    const decommissioned = await assignAsset(ctx, {
      assetID: decomm.assetID,
      employeeEmail: std.email,
      employeeFullName: std.fullName,
      expectedReturnDate: isoDateDaysFromNow(2),
    });
    expect(decommissioned.ok).toBeFalsy();

    const pastDate = await assignAsset(ctx, {
      assetID: (await createAsset(ctx, 'gate-past', 'Available')).assetID,
      employeeEmail: std.email,
      employeeFullName: std.fullName,
      expectedReturnDate: isoDateDaysFromNow(-1),
    });
    expect(pastDate.ok).toBeFalsy();

    const missingRequired = await callApi(ctx.api, '/api/checkouts/assign', {
      method: 'POST',
      token: ctx.adminToken,
      data: {
        assetID: available.assetID,
        employeeEmail: '',
        employeeFullName: '',
        durationDays: 1,
        expectedReturnDate: '',
      },
    });
    expect(missingRequired.ok).toBeFalsy();
  });

  test('5) Return restores Available and clears user list entry', async () => {
    const std = await registerUser(ctx, 'return');
    const stdRow = await findUserByEmail(ctx, std.email);
    expect(stdRow).toBeTruthy();
    const approveRes = await approveUser(ctx, (stdRow as UserSummary).userID);
    expect(approveRes.ok).toBeTruthy();

    const stdLogin = await login(ctx.api, std.email, PASSWORD);
    expect(stdLogin.ok).toBeTruthy();
    const stdToken = stdLogin.body?.token as string;

    const available = (await getAssets(ctx)).find((a) => a.status === 'Available') ?? (await createAsset(ctx, 'return-avail', 'Available'));

    const assignRes = await assignAsset(ctx, {
      assetID: available.assetID,
      employeeEmail: std.email,
      employeeFullName: std.fullName,
      expectedReturnDate: isoDateDaysFromNow(1),
    });
    expect(assignRes.ok).toBeTruthy();

    const returnByAssetId = await callApi(ctx.api, `/api/checkouts/return/${available.assetID}`, {
      method: 'POST',
      token: stdToken,
      data: {},
    });
    expect(returnByAssetId.ok).toBeTruthy();

    const myAssets = await callApi(ctx.api, '/api/checkouts/my-assets', {
      token: stdToken,
    });
    const list = (myAssets.body as Array<{ assetID: number }>) ?? [];
    expect(list.some((r) => r.assetID === available.assetID)).toBeFalsy();

    const assetAfter = (await getAssets(ctx)).find((a) => a.assetID === available.assetID);
    expect(assetAfter?.status).toBe('Available');

    // Explicit contract check to catch the UI mismatch candidate.
    const myAssetsAfter = await callApi(ctx.api, '/api/checkouts/my-assets', { token: stdToken });
    const checkoutRow = ((myAssetsAfter.body as Array<{ checkoutID: number }>) ?? [])[0];
    if (checkoutRow) {
      const returnByCheckoutId = await callApi(ctx.api, `/api/checkouts/${checkoutRow.checkoutID}/return`, {
        method: 'POST',
        token: stdToken,
        data: {},
      });
      expect(returnByCheckoutId.status).toBe(404);
    }
  });

  test('6) Serial uniqueness blocks duplicates including decommissioned', async () => {
    const available = (await getAssets(ctx)).find((a) => a.status === 'Available') ?? (await createAsset(ctx, 'uniq-avail', 'Available'));
    const decomm = (await getAssets(ctx)).find((a) => a.status === 'Decommissioned') ?? (await createAsset(ctx, 'uniq-decom', 'Decommissioned'));

    const duplicateActive = await callApi(ctx.api, '/api/assets', {
      method: 'POST',
      token: ctx.adminToken,
      data: {
        name: `dup-active-${ctx.stamp}`,
        category: available.category,
        serialNumber: available.serialNumber,
        value: 10,
        status: 'Available',
      },
    });
    expect(duplicateActive.ok).toBeFalsy();

    const duplicateDecom = await callApi(ctx.api, '/api/assets', {
      method: 'POST',
      token: ctx.adminToken,
      data: {
        name: `dup-decom-${ctx.stamp}`,
        category: decomm.category,
        serialNumber: decomm.serialNumber,
        value: 10,
        status: 'Available',
      },
    });
    expect(duplicateDecom.ok).toBeFalsy();
  });

  test('7) Decommission is soft delete and restore reactivates', async () => {
    const target = (await getAssets(ctx)).find((a) => a.status === 'Available') ?? (await createAsset(ctx, 'soft', 'Available'));

    const decom = await callApi(ctx.api, `/api/assets/${target.assetID}`, {
      method: 'DELETE',
      token: ctx.adminToken,
    });
    expect(decom.ok).toBeTruthy();
    ctx.decommissionedAssets.push(target.assetID);

    const afterDecom = (await getAssets(ctx)).find((a) => a.assetID === target.assetID);
    expect(afterDecom).toBeTruthy();
    expect(afterDecom?.status).toBe('Decommissioned');

    const restore = await callApi(ctx.api, `/api/assets/${target.assetID}/restore`, {
      method: 'POST',
      token: ctx.adminToken,
      data: {},
    });
    expect(restore.ok).toBeTruthy();

    const afterRestore = (await getAssets(ctx)).find((a) => a.assetID === target.assetID);
    expect(afterRestore?.status).toBe('Available');
  });

  test('8) RBAC enforced server-side (standard user denied, admin allowed)', async () => {
    const std = await registerUser(ctx, 'rbac');
    const stdRow = await findUserByEmail(ctx, std.email);
    expect(stdRow).toBeTruthy();

    const approveRes = await approveUser(ctx, (stdRow as UserSummary).userID);
    expect(approveRes.ok).toBeTruthy();

    const stdLogin = await login(ctx.api, std.email, PASSWORD);
    expect(stdLogin.ok).toBeTruthy();
    const stdToken = stdLogin.body?.token as string;

    const usersDenied = await callApi(ctx.api, '/api/users', { token: stdToken });
    const createDenied = await callApi(ctx.api, '/api/assets', {
      method: 'POST',
      token: stdToken,
      data: {
        name: `rbac-${ctx.stamp}`,
        category: 'Laptop',
        serialNumber: `RBAC-${ctx.stamp}`,
        value: 10,
        status: 'Available',
      },
    });
    const reportsDenied = await callApi(ctx.api, '/api/reports/checkout-history', { token: stdToken });

    expect([401, 403]).toContain(usersDenied.status);
    expect([401, 403]).toContain(createDenied.status);
    expect([401, 403]).toContain(reportsDenied.status);

    const adminUsers = await callApi(ctx.api, '/api/users', { token: ctx.adminToken });
    expect(adminUsers.ok).toBeTruthy();
  });

  test('9) Admin override return removes asset from user my-assets', async () => {
    const std = await registerUser(ctx, 'override');
    const stdRow = await findUserByEmail(ctx, std.email);
    expect(stdRow).toBeTruthy();

    const approveRes = await approveUser(ctx, (stdRow as UserSummary).userID);
    expect(approveRes.ok).toBeTruthy();

    const stdLogin = await login(ctx.api, std.email, PASSWORD);
    expect(stdLogin.ok).toBeTruthy();
    const stdToken = stdLogin.body?.token as string;

    const target = (await getAssets(ctx)).find((a) => a.status === 'Available') ?? (await createAsset(ctx, 'override-avail', 'Available'));

    const assignRes = await assignAsset(ctx, {
      assetID: target.assetID,
      employeeEmail: std.email,
      employeeFullName: std.fullName,
      expectedReturnDate: isoDateDaysFromNow(2),
    });
    expect(assignRes.ok).toBeTruthy();

    const forceReturn = await callApi(ctx.api, `/api/assets/${target.assetID}/admin-return`, {
      method: 'POST',
      token: ctx.adminToken,
      data: {},
    });
    expect(forceReturn.ok).toBeTruthy();

    const myAssets = await callApi(ctx.api, '/api/checkouts/my-assets', { token: stdToken });
    const list = (myAssets.body as Array<{ assetID: number }>) ?? [];
    expect(list.some((r) => r.assetID === target.assetID)).toBeFalsy();

    const today = new Date().toISOString().slice(0, 10);
    const overrideAudit = await callApi(ctx.api, '/api/checkouts/audit-trail', {
      token: ctx.adminToken,
      params: { action: 'ADMIN_OVERRIDE_RETURN', fromDate: today, toDate: today },
    });
    expect(overrideAudit.ok).toBeTruthy();
    expect(((overrideAudit.body as any[]) ?? []).length).toBeGreaterThan(0);
  });

  test('10) Return endpoint contract: assetId-based endpoint works (regression guard)', async () => {
    const std = await registerUser(ctx, 'contract');
    const stdRow = await findUserByEmail(ctx, std.email);
    expect(stdRow).toBeTruthy();

    const approveRes = await approveUser(ctx, (stdRow as UserSummary).userID);
    expect(approveRes.ok).toBeTruthy();

    const stdLogin = await login(ctx.api, std.email, PASSWORD);
    expect(stdLogin.ok).toBeTruthy();
    const stdToken = stdLogin.body?.token as string;

    const target = (await getAssets(ctx)).find((a) => a.status === 'Available') ?? (await createAsset(ctx, 'contract-avail', 'Available'));

    const assignRes = await assignAsset(ctx, {
      assetID: target.assetID,
      employeeEmail: std.email,
      employeeFullName: std.fullName,
      expectedReturnDate: isoDateDaysFromNow(1),
    });
    expect(assignRes.ok).toBeTruthy();

    const returnByAssetId = await callApi(ctx.api, `/api/checkouts/return/${target.assetID}`, {
      method: 'POST',
      token: stdToken,
      data: {},
    });
    expect(returnByAssetId.ok).toBeTruthy();

    const myAssets = await callApi(ctx.api, '/api/checkouts/my-assets', { token: stdToken });
    const list = (myAssets.body as Array<{ assetID: number }>) ?? [];
    expect(list.some((r) => r.assetID === target.assetID)).toBeFalsy();
  });
});
