import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { CheckoutService } from '../../../app/services/checkout';

describe('CheckoutService', () => {
  let service: CheckoutService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(CheckoutService);
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

  it('includes auth header in getMyAssets request', () => {
    service.getMyAssets().subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/checkouts/my-assets');
    expect(request.request.method).toBe('GET');
    expect(request.request.headers.get('Authorization')).toBe('Bearer jwt-token');
    request.flush([]);
  });

  it('posts to return endpoint with asset id', () => {
    service.returnAsset(42).subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/checkouts/return/42');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});
    request.flush({ message: 'ok' });
  });

  it('fetches audit trail from correct endpoint', () => {
    service.getAuditTrail().subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/checkouts/audit-trail');
    expect(request.request.method).toBe('GET');
    expect(request.request.headers.get('Authorization')).toBe('Bearer jwt-token');
    request.flush([]);
  });
});
