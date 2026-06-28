import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ReportService } from '../../../../app/services/report';

import { ReportsComponent } from '../../../../app/pages/reports/reports';

describe('ReportsComponent', () => {
  let component: ReportsComponent;
  let fixture: ComponentFixture<ReportsComponent>;
  let historyCalls: any[];
  let excelCalls: any[];
  let pdfCalls: any[];

  beforeEach(async () => {
    historyCalls = [];
    excelCalls = [];
    pdfCalls = [];

    await TestBed.configureTestingModule({
      imports: [ReportsComponent],
      providers: [
        {
          provide: ReportService,
          useValue: {
            getCheckoutHistory: (filters: any) => {
              historyCalls.push({ ...filters });
              return of([]);
            },
            getCategories: () => of(['Laptop']),
            getAssets: () => of([{ id: 1, name: 'Dell Laptop', serialNumber: 'ABC12345' }]),
            getUsers: () => of([{ id: 2, name: 'Jane Doe', email: 'jane@company.com' }]),
            downloadExcel: (filters: any) => {
              excelCalls.push({ ...filters });
              return of(new Blob(['excel']));
            },
            downloadPdf: (filters: any) => {
              pdfCalls.push({ ...filters });
              return of(new Blob(['pdf']));
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ReportsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('loads dropdowns and runs search on init', () => {
    historyCalls = [];

    component.ngOnInit();

    expect(component.categories).toEqual(['Laptop']);
    expect(component.assets.length).toBe(1);
    expect(component.users.length).toBe(1);
    expect(historyCalls.length).toBe(1);
  });

  it('runSearch populates rows and clears loading state', () => {
    const service = TestBed.inject(ReportService) as any;
    service.getCheckoutHistory = () => of([{ checkoutID: 1, checkoutDate: '2026-06-01', assetName: 'Dell Laptop', category: 'Laptop', serialNumber: 'ABC12345', userFullName: 'Jane Doe', userEmail: 'jane@company.com', status: 'Returned' }]);

    component.runSearch();

    expect(component.rows.length).toBe(1);
    expect(component.loading).toBe(false);
  });

  it('runSearch sets error message on failure', () => {
    const service = TestBed.inject(ReportService) as any;
    service.getCheckoutHistory = () => throwError(() => ({ error: { message: 'Failed to load report data.' } }));

    component.runSearch();

    expect(component.errorMessage).toBe('Failed to load report data.');
    expect(component.loading).toBe(false);
  });

  it('downloadExcel and downloadPdf toggle export calls', () => {
    const downloadBlobSpy = vi.spyOn(component as any, 'downloadBlob').mockImplementation(() => {});

    component.downloadExcel();
    component.downloadPdf();

    expect(excelCalls.length).toBe(1);
    expect(pdfCalls.length).toBe(1);
    expect(downloadBlobSpy).toHaveBeenCalledTimes(2);
  });
});




