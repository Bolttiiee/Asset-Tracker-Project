import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { AssetService } from '../../../app/services/asset';

describe('AssetService', () => {
  let service: AssetService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(AssetService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
    localStorage.setItem('token', 'jwt-token');
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('sends auth header when fetching assets', () => {
    service.getAssets().subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/assets');
    expect(request.request.method).toBe('GET');
    expect(request.request.headers.get('Authorization')).toBe('Bearer jwt-token');
    request.flush([]);
  });

  it('posts the correct payload when assigning an asset', () => {
    service.assignAsset(10, 'employee@company.com', 7, 'Jane Doe', '2026-06-30').subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/checkouts/assign');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      assetID: 10,
      employeeEmail: 'employee@company.com',
      durationDays: 7,
      employeeFullName: 'Jane Doe',
      expectedReturnDate: '2026-06-30',
    });
    request.flush({});
  });

  it('calls the correct return endpoint', () => {
    service.returnAsset(44).subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/checkouts/return/44');
    expect(request.request.method).toBe('POST');
    request.flush({});
  });

  it('calls admin override return and restore endpoints', () => {
    service.adminOverrideReturn(12).subscribe();
    service.restoreAsset(12).subscribe();

    const adminReturn = httpMock.expectOne('https://localhost:7175/api/assets/12/admin-return');
    expect(adminReturn.request.method).toBe('POST');
    adminReturn.flush({});

    const restore = httpMock.expectOne('https://localhost:7175/api/assets/12/restore');
    expect(restore.request.method).toBe('POST');
    restore.flush({});
  });

  it('sends maintenance payload correctly', () => {
    service.setMaintenanceStatus(9, true).subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/assets/9/maintenance');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ setMaintenance: true });
    request.flush({});
  });

  it('omits action param when audit action is All', () => {
    service.getAuditTrail('All', '2026-06-01', '2026-06-30').subscribe();

    const request = httpMock.expectOne((req) =>
      req.url === 'https://localhost:7175/api/checkouts/audit-trail'
      && !req.params.has('action')
      && req.params.get('fromDate') === '2026-06-01'
      && req.params.get('toDate') === '2026-06-30'
    );
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('includes action param when a specific audit action is requested', () => {
    service.getAuditTrail('ASSIGN').subscribe();

    const request = httpMock.expectOne((req) =>
      req.url === 'https://localhost:7175/api/checkouts/audit-trail'
      && req.params.get('action') === 'ASSIGN'
    );
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });
});
