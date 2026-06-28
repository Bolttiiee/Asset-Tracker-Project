import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { ReportService } from '../../../app/services/report';

describe('ReportService', () => {
  let service: ReportService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(ReportService);
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

  it('builds checkout history query params and omits All category', () => {
    service.getCheckoutHistory({ fromDate: '2026-06-01', toDate: '2026-06-30', category: 'All', assetId: 4, userId: 9 }).subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/reports/checkout-history?fromDate=2026-06-01&toDate=2026-06-30&assetId=4&userId=9');
    expect(request.request.method).toBe('GET');
    expect(request.request.headers.get('Authorization')).toBe('Bearer jwt-token');
    request.flush([]);
  });

  it('requests excel and pdf downloads as blobs', () => {
    service.downloadExcel({ category: 'Laptop' }).subscribe();
    service.downloadPdf({ category: 'Laptop' }).subscribe();

    const excelRequest = httpMock.expectOne('https://localhost:7175/api/reports/checkout-history/excel?category=Laptop');
    expect(excelRequest.request.responseType).toBe('blob');
    excelRequest.flush(new Blob());

    const pdfRequest = httpMock.expectOne('https://localhost:7175/api/reports/checkout-history/pdf?category=Laptop');
    expect(pdfRequest.request.responseType).toBe('blob');
    pdfRequest.flush(new Blob());
  });

  it('loads categories, assets, and users from their endpoints', () => {
    service.getCategories().subscribe();
    service.getAssets().subscribe();
    service.getUsers().subscribe();

    httpMock.expectOne('https://localhost:7175/api/reports/categories').flush([]);
    httpMock.expectOne('https://localhost:7175/api/reports/assets').flush([]);
    httpMock.expectOne('https://localhost:7175/api/reports/users').flush([]);
  });
});




